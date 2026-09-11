using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Sabat.API.Errors;
using Sabat.API.Security;
using Sabat.Application.Admin;
using Sabat.Application.Contacts;
using Sabat.Application.News;
using Sabat.Infrastructure;
using Sabat.Infrastructure.Persistence;
using Sabat.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var cloudRunPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{cloudRunPort}");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sabat API",
        Version = "v1",
        Description = "API for Sabat news and contact messages."
    });
    options.AddSecurityDefinition(AdminApiKeyOptions.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = AdminApiKeyOptions.HeaderName,
        Description = $"Administrative API key. Enter only the key value; Swagger sends it in the {AdminApiKeyOptions.HeaderName} header."
    });
    options.AddSecurityDefinition(AdminJwtOptions.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = builder.Configuration[$"{AdminJwtOptions.SectionName}:CookieName"] ?? "sabat_admin_session",
        Description = "Administrator session cookie created by POST /api/admin/auth/login."
    });
    options.OperationFilter<AdminApiKeyOperationFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddSingleton<IAdminTokenService, JwtAdminTokenService>();

builder.Services.PostConfigure<FileStorageOptions>(options =>
{
    if (!Path.IsPathRooted(options.RootPath))
    {
        options.RootPath = Path.Combine(builder.Environment.ContentRootPath, options.RootPath);
    }
});

builder.Services.Configure<AdminApiKeyOptions>(
    builder.Configuration.GetSection(AdminApiKeyOptions.SectionName));
builder.Services.AddOptions<AdminJwtOptions>()
    .Bind(builder.Configuration.GetSection(AdminJwtOptions.SectionName))
    .Validate(
        options => options.SigningKey.Length >= 32,
        "Admin JWT signing key must contain at least 32 characters.")
    .Validate(options => options.ExpirationMinutes is >= 15 and <= 1440,
        "Admin JWT expiration must be between 15 and 1440 minutes.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(AdminJwtOptions.SectionName)
    .Get<AdminJwtOptions>() ?? new AdminJwtOptions();
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AdminApiKeyOptions.SelectorSchemeName;
        options.DefaultChallengeScheme = AdminApiKeyOptions.SelectorSchemeName;
    })
    .AddPolicyScheme(
        AdminApiKeyOptions.SelectorSchemeName,
        AdminApiKeyOptions.SelectorSchemeName,
        options => options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey(AdminApiKeyOptions.HeaderName)
                ? AdminApiKeyOptions.SchemeName
                : AdminJwtOptions.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminApiKeyAuthenticationHandler>(
        AdminApiKeyOptions.SchemeName,
        _ => { })
    .AddJwtBearer(AdminJwtOptions.SchemeName, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKey,
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[jwtOptions.CookieName];
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionValue = context.Principal?.FindFirstValue(AdminJwtOptions.SessionVersionClaim);
                if (!Guid.TryParse(idValue, out var adminId) ||
                    !int.TryParse(versionValue, out var sessionVersion))
                {
                    context.Fail("The administrator session is invalid.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var isValid = await dbContext.AdminUsers.AsNoTracking().AnyAsync(
                    admin => admin.Id == adminId &&
                             admin.IsActive &&
                             admin.SessionVersion == sessionVersion,
                    context.HttpContext.RequestAborted);
                if (!isValid)
                {
                    context.Fail("The administrator session has been revoked.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminApiKeyOptions.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
    options.AddPolicy(AdminApiKeyOptions.ApiKeyPolicyName, policy =>
    {
        policy.AddAuthenticationSchemes(AdminApiKeyOptions.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
    options.AddPolicy(AdminApiKeyOptions.SessionPolicyName, policy =>
    {
        policy.AddAuthenticationSchemes(AdminJwtOptions.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("admin-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"admin-login:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AdminCsrfMiddleware>();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sabat API v1");
    options.DocumentTitle = "Sabat API documentation";
    options.DisplayRequestDuration();
});

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Configuration.GetValue<bool>("Database:SeedInitialContent"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<InitialDataSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

public partial class Program;
