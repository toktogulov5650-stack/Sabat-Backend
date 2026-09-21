using Microsoft.EntityFrameworkCore;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;
using Sabat.Domain;

namespace Sabat.Application.News;

public sealed class NewsService(ISabatDbContext dbContext, TimeProvider timeProvider) : INewsService
{
    public async Task<PagedResult<NewsListItemDto>> GetPublishedAsync(
        string? language,
        int page,
        int pageSize,
        string? category,
        string? search,
        CancellationToken cancellationToken)
    {
        var lang = SupportedLanguages.Normalize(language);
        (page, pageSize) = NormalizePagination(page, pageSize);
        var now = timeProvider.GetUtcNow();

        var query = dbContext.NewsArticles
            .AsNoTracking()
            .Where(article =>
                article.Status == NewsStatus.Published &&
                article.PublishedAt.HasValue &&
                article.PublishedAt <= now);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(article => article.Category.ToLower() == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(article => article.Translations.Any(translation =>
                translation.Language == lang &&
                (translation.Title.ToLower().Contains(normalizedSearch) ||
                 translation.Excerpt.ToLower().Contains(normalizedSearch) ||
                 translation.Content.ToLower().Contains(normalizedSearch))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Include(article => article.Translations)
            .OrderByDescending(article => article.PublishedAt)
            .ThenByDescending(article => article.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(article => ToListItem(article, lang)).ToArray();
        return new PagedResult<NewsListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<NewsDetailsDto> GetPublishedBySlugAsync(
        string slug,
        string? language,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = SlugGenerator.Generate(slug);
        var now = timeProvider.GetUtcNow();
        var article = await dbContext.NewsArticles
            .AsNoTracking()
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item =>
                item.Slug == normalizedSlug &&
                item.Status == NewsStatus.Published &&
                item.PublishedAt.HasValue &&
                item.PublishedAt <= now,
                cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("News article was not found.");
        }

        var translation = GetTranslation(article, SupportedLanguages.Normalize(language));
        return new NewsDetailsDto(
            article.Id,
            article.Slug,
            translation.Category,
            article.Author,
            article.CoverImageUrl,
            translation.Title,
            translation.Excerpt,
            translation.Content,
            article.PublishedAt!.Value);
    }

    public async Task<PagedResult<AdminNewsDto>> GetAdminListAsync(
        string? language,
        NewsStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var query = dbContext.NewsArticles.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(article => article.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Include(article => article.Translations)
            .OrderByDescending(article => article.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var normalizedLanguage = SupportedLanguages.Contains(language)
            ? SupportedLanguages.Normalize(language)
            : null;
        var items = entities
            .Select(article => ToAdminDto(article, normalizedLanguage))
            .ToArray();

        return new PagedResult<AdminNewsDto>(items, page, pageSize, totalCount);
    }

    public async Task<AdminNewsDto> GetAdminByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await dbContext.NewsArticles
            .AsNoTracking()
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return article is null
            ? throw new NotFoundException("News article was not found.")
            : ToAdminDto(article);
    }

    public async Task<AdminNewsDto> CreateAsync(UpsertNewsRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var slug = ResolveSlug(request);
        await EnsureSlugIsUniqueAsync(slug, null, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Category = request.CategoryKey.Trim().ToLowerInvariant(),
            Author = CleanOptional(request.Author),
            CoverImageUrl = CleanOptional(request.CoverImageUrl),
            Status = request.Status,
            PublishedAt = ResolvePublishedAt(request.Status, request.PublishedAt, now),
            CreatedAt = now,
            UpdatedAt = now,
            Translations = request.Translations.Select(ToEntity).ToList()
        };

        dbContext.NewsArticles.Add(article);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminDto(article);
    }

    public async Task<AdminNewsDto> UpdateAsync(Guid id, UpsertNewsRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var article = await dbContext.NewsArticles
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("News article was not found.");
        }

        var slug = ResolveSlug(request);
        await EnsureSlugIsUniqueAsync(slug, id, cancellationToken);

        article.Slug = slug;
        article.Category = request.CategoryKey.Trim().ToLowerInvariant();
        article.Author = CleanOptional(request.Author);
        article.CoverImageUrl = CleanOptional(request.CoverImageUrl);
        article.Status = request.Status;
        article.PublishedAt = ResolvePublishedAt(request.Status, request.PublishedAt, timeProvider.GetUtcNow());
        article.UpdatedAt = timeProvider.GetUtcNow();

        var requestedLanguages = request.Translations
            .Select(item => item.Language.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedTranslations = article.Translations
            .Where(item => !requestedLanguages.Contains(item.Language))
            .ToArray();
        dbContext.NewsTranslations.RemoveRange(removedTranslations);
        foreach (var removedTranslation in removedTranslations)
        {
            article.Translations.Remove(removedTranslation);
        }

        foreach (var translationRequest in request.Translations)
        {
            var language = translationRequest.Language.Trim().ToLowerInvariant();
            var translation = article.Translations.FirstOrDefault(item => item.Language == language);
            if (translation is null)
            {
                translation = ToEntity(translationRequest);
                translation.NewsArticleId = article.Id;
                article.Translations.Add(translation);
                dbContext.NewsTranslations.Add(translation);
                continue;
            }

            translation.Category = translationRequest.Category.Trim();
            translation.Title = translationRequest.Title.Trim();
            translation.Excerpt = translationRequest.Excerpt.Trim();
            translation.Content = translationRequest.Content.Trim();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminDto(article);
    }

    public async Task<AdminNewsDto> SetStatusAsync(
        Guid id,
        SetNewsStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw ValidationError("status", "Unknown news status.");
        }

        var article = await dbContext.NewsArticles
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("News article was not found.");
        }

        if (request.Status == NewsStatus.Published)
        {
            ValidatePublishedTranslations(article.Translations);
        }

        var now = timeProvider.GetUtcNow();
        article.Status = request.Status;
        article.PublishedAt = ResolvePublishedAt(request.Status, request.PublishedAt ?? article.PublishedAt, now);
        article.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminDto(article);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await dbContext.NewsArticles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null)
        {
            throw new NotFoundException("News article was not found.");
        }

        dbContext.NewsArticles.Remove(article);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSlugIsUniqueAsync(string slug, Guid? currentId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.NewsArticles.AnyAsync(
            article => article.Slug == slug && (!currentId.HasValue || article.Id != currentId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("A news article with this slug already exists.");
        }
    }

    private static void Validate(UpsertNewsRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        AddRequiredAndLengthError(errors, "categoryKey", request.CategoryKey, 80);

        if (request.Author?.Length > 120)
        {
            AddError(errors, "author", "Author cannot exceed 120 characters.");
        }

        if (request.CoverImageUrl?.Length > 2048)
        {
            AddError(errors, "coverImageUrl", "Cover image URL cannot exceed 2048 characters.");
        }

        if (!Enum.IsDefined(request.Status))
        {
            AddError(errors, "status", "Unknown news status.");
        }

        var translations = request.Translations ?? [];
        if (translations.Count == 0)
        {
            AddError(errors, "translations", "At least one translation is required.");
        }

        foreach (var group in translations.GroupBy(item => item.Language?.ToLowerInvariant() ?? string.Empty))
        {
            if (!SupportedLanguages.Contains(group.Key))
            {
                AddError(errors, "translations", $"Unsupported language '{group.Key}'.");
            }

            if (group.Count() > 1)
            {
                AddError(errors, "translations", $"Language '{group.Key}' is duplicated.");
            }
        }

        foreach (var translation in translations)
        {
            var key = $"translations.{translation.Language}";
            AddRequiredAndLengthError(errors, $"{key}.category", translation.Category, 80);
            AddRequiredAndLengthError(errors, $"{key}.title", translation.Title, 240);
            AddRequiredAndLengthError(errors, $"{key}.excerpt", translation.Excerpt, 600);
            AddRequiredAndLengthError(errors, $"{key}.content", translation.Content, 50_000);
        }

        if (request.Status == NewsStatus.Published)
        {
            foreach (var language in SupportedLanguages.All)
            {
                if (!translations.Any(item => string.Equals(item.Language, language, StringComparison.OrdinalIgnoreCase)))
                {
                    AddError(errors, "translations", $"Translation '{language}' is required before publishing.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors.ToDictionary(item => item.Key, item => item.Value.ToArray()));
        }
    }

    private static void ValidatePublishedTranslations(IEnumerable<NewsTranslation> translations)
    {
        var languages = translations.Select(item => item.Language).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = SupportedLanguages.All.Where(language => !languages.Contains(language)).ToArray();
        if (missing.Length > 0)
        {
            throw ValidationError("translations", $"Translations are missing: {string.Join(", ", missing)}.");
        }
    }

    private static string ResolveSlug(UpsertNewsRequest request)
    {
        var source = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Translations.FirstOrDefault(item => item.Language.Equals("ru", StringComparison.OrdinalIgnoreCase))?.Title
                ?? request.Translations.FirstOrDefault()?.Title
            : request.Slug;
        var slug = SlugGenerator.Generate(source ?? string.Empty);

        if (slug.Length is < 3 or > 180)
        {
            throw ValidationError("slug", "Slug must contain between 3 and 180 URL-safe characters.");
        }

        return slug;
    }

    private static DateTimeOffset? ResolvePublishedAt(NewsStatus status, DateTimeOffset? requested, DateTimeOffset now)
    {
        var publishedAt = status == NewsStatus.Published ? requested ?? now : requested;
        return publishedAt?.ToUniversalTime();
    }

    private static NewsTranslation ToEntity(NewsTranslationRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Language = request.Language.Trim().ToLowerInvariant(),
        Category = request.Category.Trim(),
        Title = request.Title.Trim(),
        Excerpt = request.Excerpt.Trim(),
        Content = request.Content.Trim()
    };

    private static NewsListItemDto ToListItem(NewsArticle article, string language)
    {
        var translation = GetTranslation(article, language);
        return new NewsListItemDto(
            article.Id,
            article.Slug,
            translation.Category,
            article.Author,
            article.CoverImageUrl,
            translation.Title,
            translation.Excerpt,
            article.PublishedAt!.Value);
    }

    private static AdminNewsDto ToAdminDto(NewsArticle article, string? language = null)
    {
        var translations = article.Translations
            .Where(item => language is null || item.Language == language)
            .OrderBy(item => Array.IndexOf(SupportedLanguages.All, item.Language))
            .Select(item => new NewsTranslationDto(item.Language, item.Category, item.Title, item.Excerpt, item.Content))
            .ToArray();

        return new AdminNewsDto(
            article.Id,
            article.Slug,
            article.Category,
            article.Author,
            article.CoverImageUrl,
            article.Status,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            translations);
    }

    private static NewsTranslation GetTranslation(NewsArticle article, string language) =>
        article.Translations.FirstOrDefault(item => item.Language == language)
        ?? article.Translations.FirstOrDefault(item => item.Language == "ru")
        ?? article.Translations.First();

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, 50));

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddRequiredAndLengthError(
        IDictionary<string, List<string>> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(errors, key, "Value is required.");
        }
        else if (value.Length > maxLength)
        {
            AddError(errors, key, $"Value cannot exceed {maxLength} characters.");
        }
    }

    private static void AddError(IDictionary<string, List<string>> errors, string key, string error)
    {
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors[key] = values;
        }

        values.Add(error);
    }

    private static RequestValidationException ValidationError(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
