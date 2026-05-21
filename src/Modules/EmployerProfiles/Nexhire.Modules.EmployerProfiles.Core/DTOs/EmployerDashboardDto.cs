namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record EmployerDashboardDto(
    int ActivePostingsCount,
    int TotalApplicationsCount,
    int TotalMatchesCount);
