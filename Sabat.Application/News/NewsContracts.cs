using Sabat.Application.Common;
using Sabat.Domain;

namespace Sabat.Application.News;

public sealed class NewsTranslationRequest
{
    public string Language { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed class UpsertNewsRequest
{
    public string? Slug { get; init; }
    public string CategoryKey { get; init; } = string.Empty;
    public string? Author { get; init; }
    public string? CoverImageUrl { get; init; }
    public NewsStatus Status { get; init; } = NewsStatus.Draft;
    public DateTimeOffset? PublishedAt { get; init; }
    public IReadOnlyList<NewsTranslationRequest> Translations { get; init; } = [];
}

public sealed class SetNewsStatusRequest
{
    public NewsStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}

public sealed record NewsListItemDto(
    Guid Id,
    string Slug,
    string Category,
    string? Author,
    string? CoverImageUrl,
    string Title,
    string Excerpt,
    DateTimeOffset PublishedAt);

public sealed record NewsDetailsDto(
    Guid Id,
    string Slug,
    string Category,
    string? Author,
    string? CoverImageUrl,
    string Title,
    string Excerpt,
    string Content,
    DateTimeOffset PublishedAt);

public sealed record NewsTranslationDto(
    string Language,
    string Category,
    string Title,
    string Excerpt,
    string Content);

public sealed record AdminNewsDto(
    Guid Id,
    string Slug,
    string CategoryKey,
    string? Author,
    string? CoverImageUrl,
    NewsStatus Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<NewsTranslationDto> Translations);

public interface INewsService
{
    Task<PagedResult<NewsListItemDto>> GetPublishedAsync(string? language, int page, int pageSize, string? category, string? search, CancellationToken cancellationToken);
    Task<NewsDetailsDto> GetPublishedBySlugAsync(string slug, string? language, CancellationToken cancellationToken);
    Task<PagedResult<AdminNewsDto>> GetAdminListAsync(string? language, NewsStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminNewsDto> GetAdminByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminNewsDto> CreateAsync(UpsertNewsRequest request, CancellationToken cancellationToken);
    Task<AdminNewsDto> UpdateAsync(Guid id, UpsertNewsRequest request, CancellationToken cancellationToken);
    Task<AdminNewsDto> SetStatusAsync(Guid id, SetNewsStatusRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
