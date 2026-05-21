namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;

public class DashboardApplication
{
    public Guid ApplicationId { get; set; }
    public Guid EmployerUserId { get; set; }
    public Guid PostingId { get; set; }
    public Guid JobSeekerId { get; set; }
    public DateTime SubmittedOnUtc { get; set; }
}
