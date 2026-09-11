using Microsoft.EntityFrameworkCore;
using Sabat.Application.Abstractions;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), ISabatDbContext
{
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsTranslation> NewsTranslations => Set<NewsTranslation>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
