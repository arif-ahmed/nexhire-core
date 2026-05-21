namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;

public class DashboardMatchedCandidate
{
    public Guid Id { get; set; }
    public Guid EmployerUserId { get; set; }
    public Guid PostingId { get; set; }
    public Guid CandidateUserId { get; set; }
    public int MatchScore { get; set; }
    public DateTime GeneratedOnUtc { get; set; }
}
