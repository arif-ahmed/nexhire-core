namespace Nexhire.Modules.EmployerProfiles.Contracts;

public interface IEmployerProfilePublicApi
{
    Task<bool> IsVerifiedAsync(Guid employerUserId, CancellationToken ct = default);
    Task<EmployerProfileSummaryDto?> GetSummaryAsync(Guid employerUserId, CancellationToken ct = default);
}

public record EmployerProfileSummaryDto(
    Guid EmployerProfileId,
    Guid UserId,
    string CompanyName,
    string Status,
    bool IsVerified,
    string? LogoStorageKey,
    string? Industry);
