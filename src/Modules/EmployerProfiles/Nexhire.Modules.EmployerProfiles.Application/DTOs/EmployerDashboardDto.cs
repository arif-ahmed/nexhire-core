namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record EmployerDashboardDto(
    int ActivePostingsCount,
    int TotalApplicationsCount,
    int TotalMatchesCount);
