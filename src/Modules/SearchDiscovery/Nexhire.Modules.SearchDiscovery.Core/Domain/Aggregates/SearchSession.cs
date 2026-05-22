using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;

public class SearchSession : AggregateRoot<Guid>
{
    private readonly List<Guid> _dismissedRecommendationPostingIds = [];

    public Guid SeekerUserId { get; private set; }
    public SearchCriteria? LastCriteria { get; private set; }
    public IReadOnlyCollection<Guid> DismissedRecommendationPostingIds => _dismissedRecommendationPostingIds.AsReadOnly();
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private SearchSession() { }

    public static Result<SearchSession> Start(Guid seekerUserId, DateTime nowUtc)
    {
        var session = new SearchSession
        {
            Id = Guid.NewGuid(),
            SeekerUserId = seekerUserId,
            ExpiresOnUtc = nowUtc.AddHours(24),
            UpdatedOnUtc = nowUtc
        };

        return Result.Success(session);
    }

    public void RememberCriteria(SearchCriteria criteria, DateTime nowUtc)
    {
        LastCriteria = criteria;
        SlideExpiry(nowUtc);
    }

    public void DismissRecommendation(Guid postingId, DateTime nowUtc)
    {
        if (!_dismissedRecommendationPostingIds.Contains(postingId))
            _dismissedRecommendationPostingIds.Add(postingId);

        SlideExpiry(nowUtc);
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresOnUtc;

    private void SlideExpiry(DateTime nowUtc)
    {
        ExpiresOnUtc = nowUtc.AddHours(24);
        UpdatedOnUtc = nowUtc;
    }
}
