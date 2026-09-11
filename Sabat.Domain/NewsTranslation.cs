namespace Sabat.Domain;

public sealed class NewsTranslation
{
    public Guid Id { get; set; }
    public Guid NewsArticleId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NewsArticle NewsArticle { get; set; } = null!;
}
