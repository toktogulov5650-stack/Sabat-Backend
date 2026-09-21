using Google.Cloud.Storage.V1;
using Google;
using Microsoft.Extensions.Options;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;

namespace Sabat.Infrastructure.Storage;

public sealed class GoogleCloudStorageFileStorage(
    StorageClient storageClient,
    IOptions<FileStorageOptions> options) : IFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly FileStorageOptions _options = options.Value;

    public async Task<string> SaveNewsImageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["Only JPEG, PNG and WebP images are supported."]
            });
        }

        if (content.CanSeek && content.Length > _options.MaxNewsImageBytes)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Image cannot exceed {_options.MaxNewsImageBytes / 1024 / 1024} MB."]
            });
        }

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("Google Cloud Storage bucket name is not configured.");
        }

        var now = DateTimeOffset.UtcNow;
        var objectName = $"news/{now:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var destination = new Google.Apis.Storage.v1.Data.Object
        {
            Bucket = _options.BucketName,
            Name = objectName,
            ContentType = contentType,
            CacheControl = "public,max-age=31536000,immutable"
        };

        await storageClient.UploadObjectAsync(
            destination,
            content,
            cancellationToken: cancellationToken);

        var encodedObjectName = string.Join(
            "/",
            objectName.Split('/').Select(Uri.EscapeDataString));
        var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? "/api/news/images"
            : _options.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{encodedObjectName}";
    }

    public async Task<bool> CopyNewsImageToAsync(
        string objectName,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName) ||
            !objectName.StartsWith("news/", StringComparison.Ordinal) ||
            objectName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            await storageClient.DownloadObjectAsync(
                _options.BucketName,
                objectName,
                destination,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
