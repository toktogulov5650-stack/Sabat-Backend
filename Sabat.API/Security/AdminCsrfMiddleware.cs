using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Sabat.API.Security;

public sealed class AdminCsrfMiddleware(
    RequestDelegate next,
    IOptions<AdminJwtOptions> options)
{
    private readonly AdminJwtOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresCsrfValidation(context))
        {
            await next(context);
            return;
        }

        var headerToken = context.Request.Headers[AdminJwtOptions.CsrfHeaderName].ToString();
        var cookieToken = context.Request.Cookies[_options.CsrfCookieName] ?? string.Empty;
        if (!FixedTimeEquals(headerToken, cookieToken))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "CSRF validation failed",
                Detail = $"A valid {AdminJwtOptions.CsrfHeaderName} header is required for this administrative request.",
                Instance = context.Request.Path
            });
            return;
        }

        await next(context);
    }

    private static bool RequiresCsrfValidation(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/admin") ||
            HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method) ||
            context.Request.Path.Equals("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return context.User.HasClaim(
            AdminJwtOptions.AuthenticationSourceClaim,
            AdminJwtOptions.SessionSource);
    }

    private static bool FixedTimeEquals(string first, string second)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
        {
            return false;
        }

        var firstBytes = Encoding.UTF8.GetBytes(first);
        var secondBytes = Encoding.UTF8.GetBytes(second);
        return firstBytes.Length == secondBytes.Length &&
               CryptographicOperations.FixedTimeEquals(firstBytes, secondBytes);
    }
}
