using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence.Configurations;

public sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("news_articles");
        builder.HasKey(article => article.Id);
        builder.Property(article => article.Id).HasColumnName("id");
        builder.Property(article => article.Slug).HasColumnName("slug").HasMaxLength(180).IsRequired();
        builder.Property(article => article.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
        builder.Property(article => article.Author).HasColumnName("author").HasMaxLength(120);
        builder.Property(article => article.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(2048);
        builder.Property(article => article.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(article => article.PublishedAt).HasColumnName("published_at");
        builder.Property(article => article.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(article => article.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(article => article.Slug).IsUnique();
        builder.HasIndex(article => new { article.Status, article.PublishedAt });
        builder.HasIndex(article => article.Category);

        builder.HasMany(article => article.Translations)
            .WithOne(translation => translation.NewsArticle)
            .HasForeignKey(translation => translation.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
