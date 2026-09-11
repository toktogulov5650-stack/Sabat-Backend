using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence.Configurations;

public sealed class NewsTranslationConfiguration : IEntityTypeConfiguration<NewsTranslation>
{
    public void Configure(EntityTypeBuilder<NewsTranslation> builder)
    {
        builder.ToTable("news_translations");
        builder.HasKey(translation => translation.Id);
        builder.Property(translation => translation.Id).HasColumnName("id");
        builder.Property(translation => translation.NewsArticleId).HasColumnName("news_article_id");
        builder.Property(translation => translation.Language).HasColumnName("language").HasMaxLength(2).IsRequired();
        builder.Property(translation => translation.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
        builder.Property(translation => translation.Title).HasColumnName("title").HasMaxLength(240).IsRequired();
        builder.Property(translation => translation.Excerpt).HasColumnName("excerpt").HasMaxLength(600).IsRequired();
        builder.Property(translation => translation.Content).HasColumnName("content").HasColumnType("text").IsRequired();

        builder.HasIndex(translation => new { translation.NewsArticleId, translation.Language }).IsUnique();
    }
}
