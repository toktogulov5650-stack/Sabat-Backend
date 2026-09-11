using Microsoft.EntityFrameworkCore;
using Sabat.Domain;

namespace Sabat.Application.Abstractions;

public interface ISabatDbContext
{
    DbSet<NewsArticle> NewsArticles { get; }
    DbSet<NewsTranslation> NewsTranslations { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<AdminUser> AdminUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
