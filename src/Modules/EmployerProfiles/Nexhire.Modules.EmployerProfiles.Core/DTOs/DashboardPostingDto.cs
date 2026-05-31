namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record DashboardPostingDto(
    Guid PostingId,
    string Title,
    string Status,
    DateTime LastEventOnUtc);
