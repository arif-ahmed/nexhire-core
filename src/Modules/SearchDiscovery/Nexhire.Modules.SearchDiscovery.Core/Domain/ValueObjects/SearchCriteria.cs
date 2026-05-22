using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class SearchCriteria : ValueObject
{
    public string? Keyword { get; }
    public SearchFilters Filters { get; }
    public IntentHint? IntentHint { get; }
    public SortOption Sort { get; }
    public int Page { get; }
    public int PageSize { get; }

    private SearchCriteria(
        string? keyword,
        SearchFilters filters,
        IntentHint? intentHint,
        SortOption sort,
        int page,
        int pageSize)
    {
        Keyword = keyword;
        Filters = filters;
        IntentHint = intentHint;
        Sort = sort;
        Page = page;
        PageSize = pageSize;
    }

    public static Result<SearchCriteria> Create(
        string? keyword = null,
        SearchFilters? filters = null,
        IntentHint? intentHint = null,
        SortOption sort = SortOption.Relevance,
        int page = 1,
        int pageSize = 20,
        bool allowEmptyForPersistence = false)
    {
        if (page < 1)
            return Result.Failure<SearchCriteria>(new Error("SearchCriteria.InvalidPage", "Page must be at least 1."));

        if (pageSize < 1 || pageSize > 100)
            return Result.Failure<SearchCriteria>(new Error("SearchCriteria.InvalidPageSize", "PageSize must be between 1 and 100."));

        var safeFilters = filters ?? SearchFilters.Create().Value;
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);

        if (!allowEmptyForPersistence && !hasKeyword && !safeFilters.HasAnyFilter && intentHint is null)
            return Result.Failure<SearchCriteria>(new Error("SearchCriteria.EmptyCriteria", "At least one of keyword, filters, or intent hint is required."));

        return Result.Success(new SearchCriteria(
            string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
            safeFilters,
            intentHint,
            sort,
            page,
            pageSize));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Keyword ?? string.Empty;
        yield return Filters;
        yield return IntentHint ?? (object?)null!;
        yield return Sort;
        yield return Page;
        yield return PageSize;
    }
}
