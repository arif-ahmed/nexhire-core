using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Persistence.Repositories;

internal sealed class ArticleRepository : IArticleRepository
{
    private readonly ContentManagementDbContext _db;

    public ArticleRepository(ContentManagementDbContext db) => _db = db;

    public async Task<Article?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Articles
            .Include(a => a.Localizations)
            .Include(a => a.Tags)
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Article>> GetDueForPublicationAsync(DateTime nowUtc, CancellationToken ct) =>
        await _db.Articles
            .Include(a => a.Localizations)
            .Where(a => a.Status == ArticleStatus.Scheduled)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Article>> BrowsePublishedAsync(
        Guid? categoryId, IEnumerable<string>? tags, Language language, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Articles
            .Include(a => a.Localizations)
            .Include(a => a.Tags)
            .Where(a => a.Status == ArticleStatus.Published);

        if (categoryId.HasValue)
            query = query.Where(a => a.PrimaryCategoryId == categoryId.Value);

        return await query
            .OrderByDescending(a => a.PublishedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Article>> SearchArchiveAsync(
        string query, Language language, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct)
    {
        var normalized = query.Trim().ToLower();
        var q = _db.Articles
            .Include(a => a.Localizations)
            .Where(a => a.Status != ArticleStatus.Draft);

        if (dateFrom.HasValue)
            q = q.Where(a => a.PublishedOnUtc >= dateFrom.Value);
        if (dateTo.HasValue)
            q = q.Where(a => a.PublishedOnUtc <= dateTo.Value);

        return await q
            .OrderByDescending(a => a.PublishedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct) =>
        await _db.Articles.CountAsync(a => a.PrimaryCategoryId == categoryId, ct);

    public async Task AddAsync(Article article, CancellationToken ct) =>
        await _db.Articles.AddAsync(article, ct);

    public void Update(Article article) => _db.Articles.Update(article);
    public void Delete(Article article) => _db.Articles.Remove(article);
}

internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly ContentManagementDbContext _db;

    public CategoryRepository(ContentManagementDbContext db) => _db = db;

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Categories.FindAsync([id], ct);

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct) =>
        await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct) =>
        await _db.Categories.ToListAsync(ct);

    public async Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId, CancellationToken ct) =>
        await _db.Categories.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value), ct);

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await _db.Categories.AddAsync(category, ct);

    public void Update(Category category) => _db.Categories.Update(category);
    public void Delete(Category category) => _db.Categories.Remove(category);
}

internal sealed class FaqEntryRepository : IFaqEntryRepository
{
    private readonly ContentManagementDbContext _db;

    public FaqEntryRepository(ContentManagementDbContext db) => _db = db;

    public async Task<FaqEntry?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.FaqEntries
            .Include(f => f.Localizations)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FaqEntry>> BrowsePublishedAsync(string? viewerRole, Language language, CancellationToken ct) =>
        await _db.FaqEntries
            .Include(f => f.Localizations)
            .Where(f => f.Status == ContentStatus.Published)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FaqEntry>> SearchPublishedAsync(string query, string? viewerRole, Language language, CancellationToken ct) =>
        await _db.FaqEntries
            .Include(f => f.Localizations)
            .Where(f => f.Status == ContentStatus.Published)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FaqEntry>> GetByContextKeyAsync(string contextKey, string? viewerRole, Language language, CancellationToken ct) =>
        await _db.FaqEntries
            .Include(f => f.Localizations)
            .Where(f => f.Status == ContentStatus.Published)
            .ToListAsync(ct);

    public async Task AddAsync(FaqEntry entry, CancellationToken ct) =>
        await _db.FaqEntries.AddAsync(entry, ct);

    public void Update(FaqEntry entry) => _db.FaqEntries.Update(entry);
    public void Delete(FaqEntry entry) => _db.FaqEntries.Remove(entry);
}

internal sealed class TopicRepository : ITopicRepository
{
    private readonly ContentManagementDbContext _db;

    public TopicRepository(ContentManagementDbContext db) => _db = db;

    public async Task<Topic?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Topics.FindAsync([id], ct);

    public async Task<IReadOnlyList<Topic>> GetAllAsync(CancellationToken ct) =>
        await _db.Topics.ToListAsync(ct);

    public async Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId, CancellationToken ct) =>
        await _db.Topics.AnyAsync(t => t.Slug == slug && (!excludeId.HasValue || t.Id != excludeId.Value), ct);

    public async Task<int> CountReferencingEntriesAsync(Guid topicId, CancellationToken ct) =>
        0; // Placeholder — will be implemented with actual FAQ join query

    public async Task AddAsync(Topic topic, CancellationToken ct) =>
        await _db.Topics.AddAsync(topic, ct);

    public void Update(Topic topic) => _db.Topics.Update(topic);
    public void Delete(Topic topic) => _db.Topics.Remove(topic);
}

internal sealed class GuidedTourRepository : IGuidedTourRepository
{
    private readonly ContentManagementDbContext _db;

    public GuidedTourRepository(ContentManagementDbContext db) => _db = db;

    public async Task<GuidedTour?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.GuidedTours
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<GuidedTour>> GetActiveAsync(Audience audience, Language language, CancellationToken ct) =>
        await _db.GuidedTours
            .Include(t => t.Steps)
            .Where(t => t.IsActive)
            .ToListAsync(ct);

    public async Task AddAsync(GuidedTour tour, CancellationToken ct) =>
        await _db.GuidedTours.AddAsync(tour, ct);

    public void Update(GuidedTour tour) => _db.GuidedTours.Update(tour);
}

internal sealed class ContentPreferenceRepository : IContentPreferenceRepository
{
    private readonly ContentManagementDbContext _db;

    public ContentPreferenceRepository(ContentManagementDbContext db) => _db = db;

    public async Task<ContentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _db.ContentPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct) =>
        await _db.ContentPreferences.AnyAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(ContentPreference preference, CancellationToken ct) =>
        await _db.ContentPreferences.AddAsync(preference, ct);

    public void Update(ContentPreference preference) => _db.ContentPreferences.Update(preference);
}

internal sealed class HelpFeedbackRepository : IHelpFeedbackRepository
{
    private readonly ContentManagementDbContext _db;

    public HelpFeedbackRepository(ContentManagementDbContext db) => _db = db;

    public async Task AddAsync(HelpFeedback feedback, CancellationToken ct) =>
        await _db.HelpFeedbacks.AddAsync(feedback, ct);

    public async Task<IReadOnlyList<HelpFeedback>> GetByEntryAsync(Guid faqEntryId, CancellationToken ct) =>
        await _db.HelpFeedbacks.Where(f => f.FaqEntryId == faqEntryId).ToListAsync(ct);
}
