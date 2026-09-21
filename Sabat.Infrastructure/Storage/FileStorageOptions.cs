namespace Sabat.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local";
    public string BucketName { get; set; } = string.Empty;
    public string RootPath { get; set; } = "wwwroot/uploads";
    public string PublicBaseUrl { get; set; } = string.Empty;
    public long MaxNewsImageBytes { get; set; } = 5 * 1024 * 1024;
}
