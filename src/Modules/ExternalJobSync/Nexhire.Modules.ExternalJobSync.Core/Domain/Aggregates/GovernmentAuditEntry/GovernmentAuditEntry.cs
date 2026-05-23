using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;

public enum AuditDirection { Query, Response }

public sealed class GovernmentAuditEntry : AggregateRoot<Guid>
{
    public Guid? VerificationRequestId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string RegistryName { get; private set; } = null!;
    public AuditDirection Direction { get; private set; }
    public string QueryParameters { get; private set; } = null!; // JSON
    public string ResultCode { get; private set; } = null!;
    public int? ResponseSizeBytes { get; private set; }
    public string? TransformationsApplied { get; private set; }
    public string ConsentStatusAtTime { get; private set; } = null!;
    public string IntegrityHash { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }

    private GovernmentAuditEntry() { }

    private GovernmentAuditEntry(
        Guid id, 
        Guid? verificationRequestId, 
        Guid? actorId, 
        string registryName, 
        AuditDirection direction, 
        string queryParameters, 
        string resultCode, 
        int? responseSizeBytes, 
        string? transformationsApplied, 
        string consentStatusAtTime, 
        string integrityHash, 
        DateTime occurredOnUtc) : base(id)
    {
        VerificationRequestId = verificationRequestId;
        ActorId = actorId;
        RegistryName = registryName;
        Direction = direction;
        QueryParameters = queryParameters;
        ResultCode = resultCode;
        ResponseSizeBytes = responseSizeBytes;
        TransformationsApplied = transformationsApplied;
        ConsentStatusAtTime = consentStatusAtTime;
        IntegrityHash = integrityHash;
        OccurredOnUtc = occurredOnUtc;
    }

    public static Result<GovernmentAuditEntry> Record(
        Guid? verificationRequestId, 
        Guid? actorId, 
        string registryName, 
        AuditDirection direction, 
        string queryParameters, 
        string resultCode, 
        int? responseSizeBytes, 
        string? transformationsApplied, 
        string consentStatusAtTime, 
        string previousEntryHash)
    {
        if (string.IsNullOrWhiteSpace(registryName))
            return Result.Failure<GovernmentAuditEntry>(new Error("Audit.RegistryRequired", "Registry name is required."));
        if (string.IsNullOrWhiteSpace(queryParameters))
            return Result.Failure<GovernmentAuditEntry>(new Error("Audit.QueryParametersRequired", "Query parameters are required."));
        if (string.IsNullOrWhiteSpace(resultCode))
            return Result.Failure<GovernmentAuditEntry>(new Error("Audit.ResultCodeRequired", "Result code is required."));
        if (string.IsNullOrWhiteSpace(consentStatusAtTime))
            return Result.Failure<GovernmentAuditEntry>(new Error("Audit.ConsentStatusRequired", "Consent status at time is required."));

        var entryId = Guid.NewGuid();
        var occurredOnUtc = DateTime.UtcNow;

        // Formulate hash input: concatenate ID, registry name, direction, query params, result code, consent status, timestamp, and previous hash
        var payload = $"{entryId}|{registryName}|{direction}|{queryParameters}|{resultCode}|{consentStatusAtTime}|{occurredOnUtc:o}|{previousEntryHash ?? string.Empty}";
        var computedHash = ComputeHash(payload);

        return Result.Success(new GovernmentAuditEntry(
            entryId, 
            verificationRequestId, 
            actorId, 
            registryName.Trim(), 
            direction, 
            queryParameters.Trim(), 
            resultCode.Trim(), 
            responseSizeBytes, 
            transformationsApplied?.Trim(), 
            consentStatusAtTime.Trim(), 
            computedHash, 
            occurredOnUtc));
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // No mutators (updates/deletes) exist since it is write-once, tamper-resistant
}
