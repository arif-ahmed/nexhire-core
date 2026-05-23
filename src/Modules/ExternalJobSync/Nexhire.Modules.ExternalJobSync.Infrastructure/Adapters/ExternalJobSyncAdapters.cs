using System.Security.Cryptography;
using System.Text;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Adapters;

public sealed class CredentialEncryptionAdapter : ICredentialEncryptionPort
{
    private static readonly byte[] DevKey = SHA256.HashData(Encoding.UTF8.GetBytes("NexhireDevSecretEncryptionKey2026")); // 256-bit key

    public Result<EncryptedCredentials> Encrypt(string plaintext)
    {
        try
        {
            byte[] iv = new byte[12]; // GCM standard IV size
            RandomNumberGenerator.Fill(iv);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16]; // GCM standard tag size

            using var aesGcm = new AesGcm(DevKey, tag.Length);
            aesGcm.Encrypt(iv, plaintextBytes, ciphertext, tag);

            // Combine IV, Tag, CipherText into a single string for storage
            var combined = $"{Convert.ToBase64String(iv)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
            
            return EncryptedCredentials.Create(combined, "dev-key-1");
        }
        catch (Exception ex)
        {
            return Result.Failure<EncryptedCredentials>(new Error("Encryption.Failed", $"Encryption error: {ex.Message}"));
        }
    }

    public Result<string> Decrypt(EncryptedCredentials credentials)
    {
        try
        {
            var parts = credentials.CipherText.Split(':');
            if (parts.Length != 3)
                return Result.Failure<string>(new Error("Decryption.InvalidFormat", "Invalid ciphertext format."));

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] tag = Convert.FromBase64String(parts[1]);
            byte[] ciphertext = Convert.FromBase64String(parts[2]);
            byte[] decryptedBytes = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(DevKey, tag.Length);
            aesGcm.Decrypt(iv, ciphertext, tag, decryptedBytes);

            return Result.Success(Encoding.UTF8.GetString(decryptedBytes));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(new Error("Decryption.Failed", $"Decryption error: {ex.Message}"));
        }
    }
}

public sealed class JobPostingPublicApiStub : IJobPostingPublicApi
{
    public Task<JobPostingStatusDto?> GetStatusAsync(Guid postingId, CancellationToken cancellationToken = default)
    {
        // High-fidelity mock return
        return Task.FromResult<JobPostingStatusDto?>(new JobPostingStatusDto(postingId, "Active", DateTime.UtcNow.AddMonths(1)));
    }
}

public sealed class TaxonomyApiStub : ITaxonomyApi
{
    public Task<Result<string>> MapSkillToTaxonomyCodeAsync(string rawSkillLabel, CancellationToken cancellationToken = default)
    {
        // Map common skills to mock codes
        var labelLower = rawSkillLabel.ToLowerInvariant();
        if (labelLower.Contains("c#") || labelLower.Contains(".net"))
            return Task.FromResult(Result.Success("SKL-NET-09"));
        if (labelLower.Contains("react") || labelLower.Contains("typescript"))
            return Task.FromResult(Result.Success("SKL-TS-04"));

        return Task.FromResult(Result.Success($"SKL-GEN-{rawSkillLabel.GetHashCode() % 100}"));
    }

    public Task<bool> IsValidTaxonomyCodeAsync(string taxonomyCode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(taxonomyCode.StartsWith("SKL-"));
    }
}

public sealed class TokenValidationApiStub : ITokenValidationApi
{
    public Task<Result<ValidatedToken>> ValidateAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        if (bearerToken == "valid-admin-token")
        {
            return Task.FromResult(Result.Success(new ValidatedToken(Guid.NewGuid(), "Admin", new() { "read", "write", "admin" })));
        }
        if (bearerToken == "valid-engineer-token")
        {
            return Task.FromResult(Result.Success(new ValidatedToken(Guid.NewGuid(), "Engineer", new() { "read", "write", "engineer" })));
        }

        return Task.FromResult(Result.Failure<ValidatedToken>(new Error("E-API-UNAUTHORIZED", "Invalid OAuth access token.")));
    }
}

public sealed class ExternalPortalPortStub : IExternalPortalPort
{
    public Task<Result<List<string>>> FetchJobsAsync(string endpoint, EncryptedCredentials credentials, DateTime? since = null, CancellationToken cancellationToken = default)
    {
        // Return 2 mock raw job payloads in JSON format
        var job1 = "{\"version\":\"v1\",\"external_job_id\":\"job-portal-101\",\"title\":\"Senior .NET Core Architect\",\"description\":\"Expert clean coder wanted.\",\"location\":\"San Francisco\",\"employment_type\":\"FullTime\"}";
        var job2 = "{\"version\":\"v1\",\"external_job_id\":\"job-portal-102\",\"title\":\"Frontend React Engineer\",\"description\":\"Beautiful UI designer wanted.\",\"location\":\"New York\",\"employment_type\":\"FullTime\"}";

        return Task.FromResult(Result.Success(new List<string> { job1, job2 }));
    }

    public Task<Result<string>> PushJobAsync(string endpoint, EncryptedCredentials credentials, string foreignPayload, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success("jp_ext_mock_1029"));
    }

    public Task<Result> HealthCheckAsync(string endpoint, EncryptedCredentials credentials, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}

public sealed class GovernmentRegistryPortStub : IGovernmentRegistryPort
{
    public Task<Result<VerificationResult>> VerifyAsync(Registry registry, VerificationKind kind, MinimisedRequestPayload payload, CancellationToken cancellationToken = default)
    {
        if (kind == VerificationKind.Identity)
        {
            if (payload.Fields.TryGetValue("id_number", out var idNum) && idNum == "invalid-id")
            {
                return Task.FromResult(Result.Success(VerificationResult.Create(VerificationOutcome.NoMatch, null, registry.Name, DateTime.UtcNow).Value));
            }
            return Task.FromResult(Result.Success(VerificationResult.Create(VerificationOutcome.Match, null, registry.Name, DateTime.UtcNow).Value));
        }

        if (kind == VerificationKind.Education)
        {
            return Task.FromResult(Result.Success(VerificationResult.Create(VerificationOutcome.Match, "DEG-10294", registry.Name, DateTime.UtcNow).Value));
        }

        if (kind == VerificationKind.Employer)
        {
            return Task.FromResult(Result.Success(VerificationResult.Create(VerificationOutcome.Match, "EMP-REC-94", registry.Name, DateTime.UtcNow).Value));
        }

        return Task.FromResult(Result.Failure<VerificationResult>(new Error("E-GOV-ERROR", "Internal Registry failure.")));
    }

    public Task<Result<string>> QueryEnrichmentAsync(Registry registry, string subjectKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success("{\"mol_status\":\"ActiveUser\",\"work_permit\":\"Valid\"}"));
    }
}

public sealed class GeocodingPortStub : IGeocodingPort
{
    public Task<Result<NormalisedLocation>> NormaliseAsync(string rawLocation, CancellationToken cancellationToken = default)
    {
        var city = "San Francisco";
        var country = "USA";

        if (rawLocation.ToLowerInvariant().Contains("new york"))
        {
            city = "New York";
        }
        else if (rawLocation.ToLowerInvariant().Contains("london"))
        {
            city = "London";
            country = "UK";
        }

        return Task.FromResult(NormalisedLocation.Create(city, null, country, 37.7749, -122.4194));
    }
}
