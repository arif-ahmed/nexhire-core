using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Ports;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.SearchJobs;

public class SearchJobsQueryHandler : IQueryHandler<SearchJobsQuery, SearchResultDto>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly ISearchSessionRepository _sessionRepo;
    private readonly IRecommendationQueryApi _recommendationApi;
    private readonly IUnitOfWork _unitOfWork;

    public SearchJobsQueryHandler(
        IJobIndexEntryRepository jobIndexRepo,
        ISearchSessionRepository sessionRepo,
        IRecommendationQueryApi recommendationApi,
        IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _sessionRepo = sessionRepo;
        _recommendationApi = recommendationApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SearchResultDto>> Handle(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var criteriaResult = SearchCriteria.Create(keyword: request.Keyword);
        if (criteriaResult.IsFailure)
            return Result.Failure<SearchResultDto>(criteriaResult.Error);

        var criteria = SearchCriteriaInterpreter.Apply(criteriaResult.Value, null);
        var entries = await _jobIndexRepo.SearchAsync(criteria, cancellationToken);
        var totalCount = await _jobIndexRepo.CountAsync(criteria, cancellationToken);

        var weights = RelevanceWeights.Default;
        var scored = RelevanceRanker.Rank(criteria.Keyword, criteria.IntentHint, entries, weights);

        var matchScores = new Dictionary<Guid, int>();
        var recommendations = new List<RankedJobDto>();

        if (request.SeekerUserId.HasValue)
        {
            var postingIds = scored.Select(s => s.EntryId).ToList();
            var scoresResult = await _recommendationApi.GetMatchScoresAsync(request.SeekerUserId.Value, postingIds, cancellationToken);
            if (scoresResult.IsSuccess)
                matchScores = scoresResult.Value as Dictionary<Guid, int> ?? scoresResult.Value.ToDictionary();

            var recsResult = await _recommendationApi.GetRecommendedPostingIdsAsync(request.SeekerUserId.Value, 5, cancellationToken);
            if (recsResult.IsSuccess && recsResult.Value.Count > 0)
            {
                var recEntries = entries.Where(e => recsResult.Value.Contains(e.Id)).ToList();
                var dismissedIds = await GetDismissedIds(request.SeekerUserId.Value, cancellationToken);
                foreach (var rec in recEntries.Where(e => !dismissedIds.Contains(e.Id)))
                {
                    recommendations.Add(MapToDto(rec, 0, matchScores.GetValueOrDefault(rec.Id), true));
                }
            }
        }

        var blended = ResultRankBlender.Blend(scored, matchScores, criteria.Sort);
        var items = blended.Select(b =>
        {
            var entry = entries.First(e => e.Id == b.EntryId);
            return MapToDto(entry, b.FinalScore, matchScores.GetValueOrDefault(b.EntryId), false);
        }).ToList();

        var result = new SearchResultDto(items, recommendations, criteria.Page, criteria.PageSize, totalCount, criteria.Sort.ToString(), totalCount == 0);

        if (request.SeekerUserId.HasValue)
        {
            await RememberInSession(request.SeekerUserId.Value, criteria, cancellationToken);
        }

        return Result.Success(result);
    }

    private async Task<HashSet<Guid>> GetDismissedIds(Guid seekerId, CancellationToken ct)
    {
        var session = await _sessionRepo.GetBySeekerAsync(seekerId, ct);
        return session?.DismissedRecommendationPostingIds.ToHashSet() ?? [];
    }

    private async Task RememberInSession(Guid seekerId, SearchCriteria criteria, CancellationToken ct)
    {
        var session = await _sessionRepo.GetBySeekerAsync(seekerId, ct);
        var now = DateTime.UtcNow;

        if (session is null)
        {
            session = SearchSession.Start(seekerId, now).Value;
            await _sessionRepo.AddAsync(session, ct);
        }

        session.RememberCriteria(criteria, now);
        await _sessionRepo.UpdateAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static RankedJobDto MapToDto(Domain.Aggregates.JobIndexEntry entry, double relevanceScore, int matchScore, bool isRecommended) => new(
        entry.Id, entry.Title, entry.Summary, entry.CompanyName, entry.Skills,
        entry.EmploymentType.ToString(), entry.WorkFormat.ToString(),
        entry.SalaryMin, entry.SalaryMax, entry.SalaryCurrency,
        entry.Location.District, entry.Location.City,
        entry.PostedOnUtc, entry.ApplicationDeadlineUtc,
        relevanceScore, matchScore == 0 ? null : matchScore, isRecommended);
}
