namespace Sabat.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveNewsImageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> CopyNewsImageToAsync(
        string objectName,
        Stream destination,
        CancellationToken cancellationToken = default);
}
