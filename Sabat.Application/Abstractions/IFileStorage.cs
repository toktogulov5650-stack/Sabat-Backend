namespace Sabat.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveNewsImageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
