using Microsoft.AspNetCore.Mvc;
using Sabat.Application.Abstractions;

namespace Sabat.API.Controllers;

[ApiController]
[Route("api/news/images")]
public sealed class NewsImagesController(IFileStorage fileStorage) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    [HttpGet("{**objectName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task Get(string? objectName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectName) ||
            !objectName.StartsWith("news/", StringComparison.Ordinal) ||
            objectName.Contains("..", StringComparison.Ordinal) ||
            !ContentTypes.TryGetValue(Path.GetExtension(objectName), out var contentType))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.ContentType = contentType;
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        var found = await fileStorage.CopyNewsImageToAsync(
            objectName,
            Response.Body,
            cancellationToken);
        if (!found)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
