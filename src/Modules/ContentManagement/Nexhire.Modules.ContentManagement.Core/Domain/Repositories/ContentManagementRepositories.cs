using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Repositories;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Article>> GetDueForPublicationAsync(DateTime nowUtc, CancellationToken ct);
    Task<IReadOnlyList<Article>> BrowsePublishedAsync(Guid? categoryId, IEnumerable<string>? tags, Language language, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Article>> SearchArchiveAsync(string query, Language language, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct);
    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct);
    Task AddAsync(Article article, CancellationToken ct);
    void Update(Article article);
    void Delete(Article article);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    void Update(Category category);
    void Delete(Category category);
}

public interface IFaqEntryRepository
{
    Task<FaqEntry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<FaqEntry>> BrowsePublishedAsync(string? viewerRole, Language language, CancellationToken ct);
    Task<IReadOnlyList<FaqEntry>> SearchPublishedAsync(string query, string? viewerRole, Language language, CancellationToken ct);
    Task<IReadOnlyList<FaqEntry>> GetByContextKeyAsync(string contextKey, string? viewerRole, Language language, CancellationToken ct);
    Task AddAsync(FaqEntry entry, CancellationToken ct);
    void Update(FaqEntry entry);
    void Delete(FaqEntry entry);
}

public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Topic>> GetAllAsync(CancellationToken ct);
    Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId, CancellationToken ct);
    Task<int> CountReferencingEntriesAsync(Guid topicId, CancellationToken ct);
    Task AddAsync(Topic topic, CancellationToken ct);
    void Update(Topic topic);
    void Delete(Topic topic);
}

public interface IGuidedTourRepository
{
    Task<GuidedTour?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<GuidedTour>> GetActiveAsync(Audience audience, Language language, CancellationToken ct);
    Task AddAsync(GuidedTour tour, CancellationToken ct);
    void Update(GuidedTour tour);
}

public interface IContentPreferenceRepository
{
    Task<ContentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(ContentPreference preference, CancellationToken ct);
    void Update(ContentPreference preference);
}

public interface IHelpFeedbackRepository
{
    Task AddAsync(HelpFeedback feedback, CancellationToken ct);
    Task<IReadOnlyList<HelpFeedback>> GetByEntryAsync(Guid faqEntryId, CancellationToken ct);
}

public interface IContentManagementUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
