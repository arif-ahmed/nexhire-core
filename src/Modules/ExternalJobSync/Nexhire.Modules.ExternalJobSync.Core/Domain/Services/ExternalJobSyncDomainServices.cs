using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Services;

public sealed class JobDataTransformer
{
    public async Task<Result<NormalisedJobPosting>> ToNormalisedAsync(
        string rawForeignPayload, 
        MappingProfile profile,
        Func<string, Task<Result<string>>> mapSkillToTaxonomy,
        Func<string, Task<Result<NormalisedLocation>>> normaliseLocation)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(rawForeignPayload);
            var root = jsonDoc.RootElement;

            string? title = null;
            string? description = null;
            NormalisedLocation? location = null;
            SalaryRange? salaryRange = null;
            string? employmentType = null;
            var requirements = new List<string>();
            var skillCodes = new List<string>();
            var sourceName = profile.PortalName;
            string? backlink = null;
            string? externalJobId = null;
            var postedOnUtc = DateTime.UtcNow;
            DateTime? deadlineUtc = null;

            foreach (var mapping in profile.FieldMappings)
            {
                var valueStr = GetJsonValueByPath(root, mapping.SourcePath);
                
                if (mapping.IsRequired && string.IsNullOrWhiteSpace(valueStr) && mapping.TransformKind != TransformKind.Constant)
                {
                    return Result.Failure<NormalisedJobPosting>(
                        new Error("E-SYNC-MISSING-FIELD", $"Required field mapping '{mapping.SourcePath}' resulted in empty value."));
                }

                if (string.IsNullOrWhiteSpace(valueStr) && mapping.TransformKind != TransformKind.Constant)
                    continue;

                switch (mapping.TransformKind)
                {
                    case TransformKind.Direct:
                        if (mapping.TargetPath == "title") title = valueStr;
                        else if (mapping.TargetPath == "description") description = valueStr;
                        else if (mapping.TargetPath == "employment_type") employmentType = valueStr;
                        else if (mapping.TargetPath == "external_job_id") externalJobId = valueStr;
                        else if (mapping.TargetPath == "backlink") backlink = valueStr;
                        break;

                    case TransformKind.Constant:
                        var constantVal = mapping.TransformArgs ?? string.Empty;
                        if (mapping.TargetPath == "employment_type") employmentType = constantVal;
                        break;

                    case TransformKind.LocationNormalise:
                        if (mapping.TargetPath == "location" && !string.IsNullOrWhiteSpace(valueStr))
                        {
                            var locResult = await normaliseLocation(valueStr);
                            if (locResult.IsFailure)
                            {
                                if (mapping.IsRequired)
                                    return Result.Failure<NormalisedJobPosting>(new Error("E-SYNC-MISSING-FIELD", "Location normalization failed for required field."));
                            }
                            else
                            {
                                location = locResult.Value;
                            }
                        }
                        break;

                    case TransformKind.SkillTaxonomyMap:
                        if (!string.IsNullOrWhiteSpace(valueStr))
                        {
                            var skillResult = await mapSkillToTaxonomy(valueStr);
                            if (skillResult.IsSuccess)
                            {
                                skillCodes.Add(skillResult.Value);
                                requirements.Add(valueStr);
                            }
                        }
                        break;

                    case TransformKind.DateParse:
                        if (!string.IsNullOrWhiteSpace(valueStr) && DateTime.TryParse(valueStr, out var parsedDate))
                        {
                            if (mapping.TargetPath == "posted_on") postedOnUtc = parsedDate.ToUniversalTime();
                            else if (mapping.TargetPath == "deadline") deadlineUtc = parsedDate.ToUniversalTime();
                        }
                        break;

                    case TransformKind.SalaryRange:
                        if (!string.IsNullOrWhiteSpace(valueStr))
                        {
                            try
                            {
                                using var salaryArgs = JsonDocument.Parse(mapping.TransformArgs ?? "{}");
                                decimal min = 0;
                                decimal max = 0;
                                string currency = "USD";

                                if (salaryArgs.RootElement.TryGetProperty("minPath", out var minP) && 
                                    root.TryGetProperty(minP.GetString() ?? "", out var minEl) && 
                                    decimal.TryParse(minEl.ToString(), out var parsedMin))
                                {
                                    min = parsedMin;
                                }

                                if (salaryArgs.RootElement.TryGetProperty("maxPath", out var maxP) && 
                                    root.TryGetProperty(maxP.GetString() ?? "", out var maxEl) && 
                                    decimal.TryParse(maxEl.ToString(), out var parsedMax))
                                {
                                    max = parsedMax;
                                }

                                if (salaryArgs.RootElement.TryGetProperty("currency", out var currEl))
                                {
                                    currency = currEl.GetString() ?? "USD";
                                }

                                var salResult = SalaryRange.Create(min, max, currency);
                                if (salResult.IsSuccess)
                                {
                                    salaryRange = salResult.Value;
                                }
                            }
                            catch
                            {
                                // Fail silently if salary range mapping format fails
                            }
                        }
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || location == null || string.IsNullOrWhiteSpace(externalJobId))
            {
                return Result.Failure<NormalisedJobPosting>(
                    new Error("E-SYNC-MISSING-FIELD", "One or more required fields (title, description, location, external_job_id) are missing."));
            }

            var extRefResult = ExternalRef.Create(profile.PortalName, externalJobId);
            if (extRefResult.IsFailure)
                return Result.Failure<NormalisedJobPosting>(extRefResult.Error);

            var attrResult = SourceAttribution.Create(sourceName, backlink, true);
            if (attrResult.IsFailure)
                return Result.Failure<NormalisedJobPosting>(attrResult.Error);

            return NormalisedJobPosting.Create(
                title, 
                description, 
                location, 
                salaryRange, 
                employmentType ?? "FullTime", 
                requirements, 
                skillCodes, 
                attrResult.Value, 
                extRefResult.Value, 
                postedOnUtc, 
                deadlineUtc);
        }
        catch (Exception ex)
        {
            return Result.Failure<NormalisedJobPosting>(new Error("E-SYNC-PARSE-ERROR", $"JSON payload parse error: {ex.Message}"));
        }
    }

    public Result<string> ToForeign(NormalisedJobPosting posting, MappingProfile outboundProfile)
    {
        try
        {
            var dict = new Dictionary<string, object>();

            foreach (var mapping in outboundProfile.FieldMappings)
            {
                object value = string.Empty;

                switch (mapping.TargetPath)
                {
                    case "title":
                        value = posting.Title;
                        break;
                    case "description":
                        value = posting.Description;
                        break;
                    case "location":
                        value = $"{posting.Location.City}, {posting.Location.Country}";
                        break;
                    case "salary":
                        value = posting.SalaryRange != null ? $"{posting.SalaryRange.Min}-{posting.SalaryRange.Max}" : "";
                        break;
                    case "employment_type":
                        value = posting.EmploymentType;
                        break;
                    default:
                        if (mapping.TransformKind == TransformKind.Constant)
                        {
                            value = mapping.TransformArgs ?? string.Empty;
                        }
                        break;
                }

                dict[mapping.SourcePath] = value;
            }

            var jsonStr = JsonSerializer.Serialize(dict);
            return Result.Success(jsonStr);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(new Error("E-SYNC-EXPORT-ERROR", $"Export transformation error: {ex.Message}"));
        }
    }

    private string? GetJsonValueByPath(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
            {
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current.ToString();
    }
}

public sealed class SchemaVersionDetector
{
    public Result<string> Detect(string rawForeignPayload, List<string> knownSchemaVersions)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawForeignPayload);
            var root = doc.RootElement;

            if (root.TryGetProperty("version", out var versionEl))
            {
                var ver = versionEl.GetString();
                if (ver != null && knownSchemaVersions.Contains(ver))
                    return Result.Success(ver);
            }

            if (root.TryGetProperty("schema_version", out var schemaEl))
            {
                var ver = schemaEl.GetString();
                if (ver != null && knownSchemaVersions.Contains(ver))
                    return Result.Success(ver);
            }

            // Fallback: Pick the first known version if only one is registered, otherwise fail
            if (knownSchemaVersions.Count == 1)
                return Result.Success(knownSchemaVersions[0]);

            return Result.Failure<string>(new Error("E-SYNC-UNKNOWN-SCHEMA", "Could not detect schema version of the payload."));
        }
        catch
        {
            return Result.Failure<string>(new Error("E-SYNC-UNKNOWN-SCHEMA", "Invalid JSON payload."));
        }
    }
}

public sealed class DuplicateJobDetector
{
    public bool IsDuplicate(
        ExternalRef incomingRef, 
        NormalisedJobPosting incomingJob,
        Func<ExternalRef, Task<bool>> exactRefExists,
        Func<string, string, string, Task<bool>> fuzzyMatchExists)
    {
        // Tier 1: Exact reference match
        var exactExists = exactRefExists(incomingRef).GetAwaiter().GetResult();
        if (exactExists)
            return true;

        // Tier 2: Fuzzy match
        var fuzzyExists = fuzzyMatchExists(incomingJob.Title, incomingJob.SourceAttribution.SourceName, incomingJob.Location.City).GetAwaiter().GetResult();
        return fuzzyExists;
    }
}

public sealed class WebhookSignatureVerifier
{
    public Result Verify(string rawBody, WebhookSignature providedSignature, EncryptedCredentials signingSecret, WebhookSigningAlgorithm algorithm)
    {
        if (algorithm == WebhookSigningAlgorithm.BearerToken)
        {
            // Plain comparison (decrypted bearer token against provided signature value)
            // Storing the signature raw comparison securely.
            return CryptographicEquals(signingSecret.CipherText, providedSignature.SignatureValue)
                ? Result.Success()
                : Result.Failure(new Error("E-WEBHOOK-SIGNATURE-INVALID", "Invalid webhook bearer token signature."));
        }

        if (algorithm == WebhookSigningAlgorithm.HmacSha256)
        {
            var keyBytes = Encoding.UTF8.GetBytes(signingSecret.CipherText);
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(bodyBytes);
            var expectedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return CryptographicEquals(expectedSignature, providedSignature.SignatureValue.ToLowerInvariant())
                ? Result.Success()
                : Result.Failure(new Error("E-WEBHOOK-SIGNATURE-INVALID", "Webhook signature verification failed."));
        }

        return Result.Failure(new Error("E-WEBHOOK-ALGORITHM-UNSUPPORTED", "Unsupported signing algorithm."));
    }

    private static bool CryptographicEquals(string a, string b)
    {
        int minLength = Math.Min(a.Length, b.Length);
        int diff = a.Length ^ b.Length;
        for (int i = 0; i < minLength; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}

public sealed class ApiKeyGenerator
{
    public (string plaintext, string keyHash, string keyPrefix) Generate()
    {
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var plaintext = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        if (plaintext.Length < 32)
        {
            plaintext += Guid.NewGuid().ToString("N");
        }

        plaintext = plaintext[..32];
        var prefix = plaintext[..8];

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        var keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (plaintext, keyHash, prefix);
    }
}

public sealed class ConsentEvaluator
{
    private static readonly Dictionary<VerificationKind, HashSet<string>> Whitelists = new()
    {
        { VerificationKind.Identity, new() { "id_number", "id_type" } },
        { VerificationKind.Education, new() { "student_id", "institution_name", "degree_name" } },
        { VerificationKind.Employer, new() { "registration_number", "tax_id" } }
    };

    public Result EnsureConsentAndMinimisation(VerificationKind kind, ConsentRecord consent, Dictionary<string, string> requestedFields)
    {
        if (!consent.Granted)
            return Result.Failure(new Error("E-GOV-CONSENT-REQUIRED", "Explicit user consent is required."));

        var whitelist = Whitelists[kind];
        foreach (var key in requestedFields.Keys)
        {
            if (!whitelist.Contains(key))
            {
                return Result.Failure(new Error("Payload.NotWhitelisted", $"Field '{key}' is not whitelisted for verification kind '{kind}'."));
            }
        }

        return Result.Success();
    }
}

public sealed class AuditHashChainer
{
    public string ComputeIntegrityHash(GovernmentAuditEntry candidate, string? previousEntryHash)
    {
        var payload = $"{candidate.Id}|{candidate.RegistryName}|{candidate.Direction}|{candidate.QueryParameters}|{candidate.ResultCode}|{candidate.ConsentStatusAtTime}|{candidate.OccurredOnUtc:o}|{previousEntryHash ?? string.Empty}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
