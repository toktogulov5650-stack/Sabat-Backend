using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Google.Cloud.Storage.V1;
using Sabat.Application.Abstractions;
using Sabat.Infrastructure.Persistence;
using Sabat.Infrastructure.Security;
using Sabat.Infrastructure.Storage;

namespace Sabat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISabatDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IAdminPasswordHasher, Pbkdf2AdminPasswordHasher>();
        services.AddScoped<InitialDataSeeder>();
        var storageSection = configuration.GetSection(FileStorageOptions.SectionName);
        services.Configure<FileStorageOptions>(options =>
        {
            options.Provider = storageSection["Provider"] ?? options.Provider;
            options.BucketName = storageSection["BucketName"] ?? options.BucketName;
            options.RootPath = storageSection["RootPath"] ?? options.RootPath;
            options.PublicBaseUrl = storageSection["PublicBaseUrl"] ?? options.PublicBaseUrl;
            if (long.TryParse(storageSection["MaxNewsImageBytes"], out var maxBytes))
            {
                options.MaxNewsImageBytes = maxBytes;
            }
        });
        var storageProvider = storageSection["Provider"] ?? "Local";
        if (storageProvider.Equals("GoogleCloudStorage", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(_ => StorageClient.Create());
            services.AddScoped<IFileStorage, GoogleCloudStorageFileStorage>();
        }
        else
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
        }
        return services;
    }
}
