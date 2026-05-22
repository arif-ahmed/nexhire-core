using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RunSavedSearch;

public class RunSavedSearchQueryHandler : IQueryHandler<RunSavedSearchQuery, SearchResultDto>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IJobIndexEntryRepository _jobIndexRepo;

    public RunSavedSearchQueryHandler(ISavedSearchRepository savedSearchRepo, IJobIndexEntryRepository jobIndexRepo)
    {
        _savedSearchRepo = savedSearchRepo;
        _jobIndexRepo = jobIndexRepo;
    }

    public async Task<Result<SearchResultDto>> Handle(RunSavedSearchQuery request, CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepo.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null)
            return Result.Failure<SearchResultDto>(new Error("E-SAVED-SEARCH-NOT-FOUND", "Saved search not found."));

        if (savedSearch.SeekerUserId != request.SeekerUserId)
            return Result.Failure<SearchResultDto>(new Error("E-FORBIDDEN", "You do not own this saved search."));

        var criteria = savedSearch.Criteria;
        var entries = await _jobIndexRepo.SearchAsync(criteria, cancellationToken);
        var totalCount = await _jobIndexRepo.CountAsync(criteria, cancellationToken);

        var scored = RelevanceRanker.Rank(criteria.Keyword, criteria.IntentHint, entries, RelevanceWeights.Default);
        var blended = ResultRankBlender.Blend(scored, new Dictionary<Guid, int>(), criteria.Sort);

        var items = blended.Select(b =>
        {
            var entry = entries.First(e => e.Id == b.EntryId);
            return new RankedJobDto(
                entry.Id, entry.Title, entry.Summary, entry.CompanyName, entry.Skills,
                entry.EmploymentType.ToString(), entry.WorkFormat.ToString(),
                entry.SalaryMin, entry.SalaryMax, entry.SalaryCurrency,
                entry.Location.District, entry.Location.City,
                entry.PostedOnUtc, entry.ApplicationDeadlineUtc,
                b.FinalScore, null, false);
        }).ToList();

        savedSearch.RecordEvaluated(DateTime.UtcNow);
        await _savedSearchRepo.UpdateAsync(savedSearch, cancellationToken);

        return Result.Success(new SearchResultDto(items, [], criteria.Page, criteria.PageSize, totalCount, criteria.Sort.ToString(), totalCount == 0));
    }
}
