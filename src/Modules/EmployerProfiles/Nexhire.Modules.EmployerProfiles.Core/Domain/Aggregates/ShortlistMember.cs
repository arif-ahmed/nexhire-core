namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

public class ShortlistMember
{
    public Guid Id { get; private set; }
    public Guid CandidateUserId { get; private set; }
    public int? MatchScore { get; private set; }
    public DateTime AddedOnUtc { get; private set; }

    private ShortlistMember(Guid id, Guid candidateUserId, int? matchScore)
    {
        Id = id;
        CandidateUserId = candidateUserId;
        MatchScore = matchScore;
        AddedOnUtc = DateTime.UtcNow;
    }

    private ShortlistMember()
    {
        // Required by EF Core
    }

    public static ShortlistMember Create(Guid id, Guid candidateUserId, int? matchScore)
    {
        return new ShortlistMember(id, candidateUserId, matchScore);
    }
}
