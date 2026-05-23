using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;

public record JobPostingStatusDto(Guid PostingId, string Status, DateTime? DeadlineUtc);
public record ValidatedToken(Guid UserId, string Role, List<string> Scopes);

public interface IJobPostingPublicApi
{
    Task<JobPostingStatusDto?> GetStatusAsync(Guid postingId, CancellationToken cancellationToken = default);
}

public interface ITaxonomyApi
{
    Task<Result<string>> MapSkillToTaxonomyCodeAsync(string rawSkillLabel, CancellationToken cancellationToken = default);
    Task<bool> IsValidTaxonomyCodeAsync(string taxonomyCode, CancellationToken cancellationToken = default);
}

public interface ITokenValidationApi
{
    Task<Result<ValidatedToken>> ValidateAsync(string bearerToken, CancellationToken cancellationToken = default);
}

public interface IExternalPortalPort
{
    Task<Result<List<string>>> FetchJobsAsync(string endpoint, EncryptedCredentials credentials, DateTime? since = null, CancellationToken cancellationToken = default);
    Task<Result<string>> PushJobAsync(string endpoint, EncryptedCredentials credentials, string foreignPayload, CancellationToken cancellationToken = default);
    Task<Result> HealthCheckAsync(string endpoint, EncryptedCredentials credentials, CancellationToken cancellationToken = default);
}

public interface IGovernmentRegistryPort
{
    Task<Result<VerificationResult>> VerifyAsync(Registry registry, VerificationKind kind, MinimisedRequestPayload payload, CancellationToken cancellationToken = default);
    Task<Result<string>> QueryEnrichmentAsync(Registry registry, string subjectKey, CancellationToken cancellationToken = default);
}

public interface IGeocodingPort
{
    Task<Result<NormalisedLocation>> NormaliseAsync(string rawLocation, CancellationToken cancellationToken = default);
}

public interface ICredentialEncryptionPort
{
    Result<EncryptedCredentials> Encrypt(string plaintext);
    Result<string> Decrypt(EncryptedCredentials credentials);
}
