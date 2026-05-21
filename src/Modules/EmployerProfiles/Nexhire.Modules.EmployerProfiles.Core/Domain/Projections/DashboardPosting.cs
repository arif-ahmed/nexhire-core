namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;

public class DashboardPosting
{
    public Guid PostingId { get; set; }
    public Guid EmployerUserId { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime LastEventOnUtc { get; set; }
}
