namespace Nexhire.Modules.ExternalJobSync.Core.DTOs;

public sealed record SyncRecordSummaryDto(
    Guid Id, 
    string Direction, 
    string PortalName, 
    string ExternalJobId, 
    Guid? InternalJobId, 
    string Status, 
    DateTime LastSyncOnUtc, 
    string? ErrorCode);

public sealed record SyncAttemptDto(
    int AttemptNo, 
    string Outcome, 
    int? ResponseCode, 
    string? Detail, 
    int ProcessingMs, 
    DateTime AttemptedOnUtc, 
    bool WasManualOverride);

public sealed record SyncRecordDetailDto(
    Guid Id, 
    string Direction, 
    string PortalName, 
    string ExternalJobId, 
    Guid? InternalJobId, 
    string Status, 
    string RawPayload, 
    string? NormalisedSnapshot, 
    string? ErrorCode, 
    string? ErrorMessage, 
    DateTime LastSyncOnUtc, 
    DateTime CreatedOnUtc, 
    List<SyncAttemptDto> Attempts);

public sealed record PartnerSyncDashboardDto(
    string PartnerName, 
    string PartnerStatus, 
    int TotalSynced, 
    int TotalQuarantined, 
    int TotalFailed, 
    DateTime? LastSyncTime);

public sealed record PartnerUsageStatsDto(
    Guid PartnerId, 
    string PartnerName, 
    int TotalRequests, 
    int ActiveKeysCount);

public sealed record IntegrationDashboardDto(
    int ActivePartnersCount, 
    int ActiveConnectorsCount, 
    int TotalSyncSuccess, 
    int TotalSyncQuarantine, 
    int TotalSyncFailed);

public sealed record VerificationStatusDto(
    Guid Id, 
    string Kind, 
    string Status, 
    string RegistryName, 
    string? Outcome, 
    string? CredentialRef, 
    string? FailureReason, 
    DateTime? CachedUntilUtc);

public sealed record GovernmentAuditEntryDto(
    Guid Id, 
    Guid? VerificationRequestId, 
    string RegistryName, 
    string Direction, 
    string QueryParameters, 
    string ResultCode, 
    string IntegrityHash, 
    DateTime OccurredOnUtc);

public sealed record ApiVersionDto(
    string Version, 
    string Status, 
    DateTime ReleasedOnUtc, 
    DateTime? SunsetOnUtc);
