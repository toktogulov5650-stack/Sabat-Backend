using Microsoft.AspNetCore.Mvc;
using Sabat.Application.Common;
using Sabat.Application.News;

namespace Sabat.API.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController(INewsService newsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<NewsListItemDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<NewsListItemDto>> GetList(
        [FromQuery] string? lang = "ru",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        newsService.GetPublishedAsync(lang, page, pageSize, category, search, cancellationToken);

    [HttpGet("latest")]
    [ProducesResponseType<PagedResult<NewsListItemDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<NewsListItemDto>> GetLatest(
        [FromQuery] string? lang = "ru",
        [FromQuery] int take = 3,
        CancellationToken cancellationToken = default) =>
        newsService.GetPublishedAsync(lang, 1, Math.Clamp(take, 1, 12), null, null, cancellationToken);

    [HttpGet("{slug}")]
    [ProducesResponseType<NewsDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<NewsDetailsDto> GetBySlug(
        string slug,
        [FromQuery] string? lang = "ru",
        CancellationToken cancellationToken = default) =>
        newsService.GetPublishedBySlugAsync(slug, lang, cancellationToken);
}
