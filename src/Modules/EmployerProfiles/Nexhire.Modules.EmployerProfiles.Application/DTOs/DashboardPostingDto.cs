namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record DashboardPostingDto(
    Guid PostingId,
    string Title,
    string Status,
    DateTime LastEventOnUtc);
