using Microsoft.Extensions.Options;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;

namespace Sabat.Infrastructure.Storage;

public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
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

        var now = DateTimeOffset.UtcNow;
        var relativeDirectory = Path.Combine("news", now.Year.ToString("0000"), now.Month.ToString("00"));
        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(AppContext.BaseDirectory, _options.RootPath);
        var directory = Path.Combine(root, relativeDirectory);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(directory, fileName);
        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await content.CopyToAsync(output, cancellationToken);

        var relativeUrl = $"/uploads/{relativeDirectory.Replace('\\', '/')}/{fileName}";
        return string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? relativeUrl
            : $"{_options.PublicBaseUrl.TrimEnd('/')}{relativeUrl}";
    }
}
