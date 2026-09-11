namespace Sabat.Domain;

public sealed class NewsArticle
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? CoverImageUrl { get; set; }
    public NewsStatus Status { get; set; } = NewsStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<NewsTranslation> Translations { get; set; } = [];
}
