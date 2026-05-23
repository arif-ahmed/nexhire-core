using MediatR;
using Nexhire.Modules.ExternalJobSync.Core.DTOs;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.CQRS.Queries;

public sealed class GetSyncRecordByExternalRefQueryHandler : IQueryHandler<GetSyncRecordByExternalRefQuery, SyncRecordSummaryDto?>
{
    private readonly ISyncRecordRepository _syncRecordRepository;

    public GetSyncRecordByExternalRefQueryHandler(ISyncRecordRepository syncRecordRepository)
    {
        _syncRecordRepository = syncRecordRepository;
    }

    public async Task<Result<SyncRecordSummaryDto?>> Handle(GetSyncRecordByExternalRefQuery request, CancellationToken cancellationToken)
    {
        var extRefResult = ExternalRef.Create(request.PortalName, request.ExternalJobId);
        if (extRefResult.IsFailure)
            return Result.Failure<SyncRecordSummaryDto?>(extRefResult.Error);

        var record = await _syncRecordRepository.GetByExternalRefAsync(extRefResult.Value, cancellationToken);
        if (record == null)
            return Result.Success<SyncRecordSummaryDto?>(null);

        var dto = new SyncRecordSummaryDto(
            record.Id,
            record.Direction.ToString(),
            record.ExternalRef.PortalName,
            record.ExternalRef.ExternalJobId,
            record.InternalJobId,
            record.Status.ToString(),
            record.LastSyncOnUtc,
            record.ErrorCode);

        return Result.Success<SyncRecordSummaryDto?>(dto);
    }
}

public sealed class GetSyncRecordDetailQueryHandler : IQueryHandler<GetSyncRecordDetailQuery, SyncRecordDetailDto?>
{
    private readonly ISyncRecordRepository _syncRecordRepository;

    public GetSyncRecordDetailQueryHandler(ISyncRecordRepository syncRecordRepository)
    {
        _syncRecordRepository = syncRecordRepository;
    }

    public async Task<Result<SyncRecordDetailDto?>> Handle(GetSyncRecordDetailQuery request, CancellationToken cancellationToken)
    {
        var record = await _syncRecordRepository.GetByIdAsync(request.SyncRecordId, cancellationToken);
        if (record == null)
            return Result.Success<SyncRecordDetailDto?>(null);

        var attempts = record.Attempts.Select(a => new SyncAttemptDto(
            a.AttemptNo,
            a.Outcome.ToString(),
            a.ResponseCode,
            a.Detail,
            a.ProcessingMs,
            a.AttemptedOnUtc,
            a.WasManualOverride)).ToList();

        var dto = new SyncRecordDetailDto(
            record.Id,
            record.Direction.ToString(),
            record.ExternalRef.PortalName,
            record.ExternalRef.ExternalJobId,
            record.InternalJobId,
            record.Status.ToString(),
            record.RawPayload,
            record.NormalisedSnapshot,
            record.ErrorCode,
            record.ErrorMessage,
            record.LastSyncOnUtc,
            record.CreatedOnUtc,
            attempts);

        return Result.Success<SyncRecordDetailDto?>(dto);
    }
}

public sealed class GetVerificationStatusQueryHandler : IQueryHandler<GetVerificationStatusQuery, VerificationStatusDto?>
{
    private readonly IVerificationRequestRepository _verificationRepository;

    public GetVerificationStatusQueryHandler(IVerificationRequestRepository verificationRepository)
    {
        _verificationRepository = verificationRepository;
    }

    public async Task<Result<VerificationStatusDto?>> Handle(GetVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        var req = await _verificationRepository.GetByIdAsync(request.VerificationRequestId, cancellationToken);
        if (req == null)
            return Result.Success<VerificationStatusDto?>(null);

        var dto = new VerificationStatusDto(
            req.Id,
            req.Kind.ToString(),
            req.Status.ToString(),
            req.Registry.Name,
            req.Result?.Outcome.ToString(),
            req.Result?.CredentialRef,
            req.FailureReason,
            req.CachedUntilUtc);

        return Result.Success<VerificationStatusDto?>(dto);
    }
}

// Stubs for remaining Queries to ensure Perfect Modular Compilation
public sealed class GetSubmissionLogsQueryHandler : IQueryHandler<GetSubmissionLogsQuery, List<SyncRecordSummaryDto>>
{
    public Task<Result<List<SyncRecordSummaryDto>>> Handle(GetSubmissionLogsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new List<SyncRecordSummaryDto>()));
}

public sealed class GetSubmissionLogDetailQueryHandler : IQueryHandler<GetSubmissionLogDetailQuery, SyncRecordDetailDto?>
{
    public Task<Result<SyncRecordDetailDto?>> Handle(GetSubmissionLogDetailQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<SyncRecordDetailDto?>(null));
}

public sealed class GetPartnerSyncDashboardQueryHandler : IQueryHandler<GetPartnerSyncDashboardQuery, PartnerSyncDashboardDto>
{
    public Task<Result<PartnerSyncDashboardDto>> Handle(GetPartnerSyncDashboardQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new PartnerSyncDashboardDto("Mock Portal", "Active", 150, 2, 0, DateTime.UtcNow)));
}

public sealed class GetPartnerUsageStatsQueryHandler : IQueryHandler<GetPartnerUsageStatsQuery, PartnerUsageStatsDto>
{
    public Task<Result<PartnerUsageStatsDto>> Handle(GetPartnerUsageStatsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new PartnerUsageStatsDto(request.PartnerId, "Mock Portal", 4200, 1)));
}

public sealed class GetIntegrationDashboardQueryHandler : IQueryHandler<GetIntegrationDashboardQuery, IntegrationDashboardDto>
{
    public Task<Result<IntegrationDashboardDto>> Handle(GetIntegrationDashboardQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new IntegrationDashboardDto(5, 2, 2300, 14, 3)));
}

public sealed class GetConnectorSyncLogQueryHandler : IQueryHandler<GetConnectorSyncLogQuery, List<SyncRecordSummaryDto>>
{
    public Task<Result<List<SyncRecordSummaryDto>>> Handle(GetConnectorSyncLogQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new List<SyncRecordSummaryDto>()));
}

public sealed class ExportSyncLogsQueryHandler : IQueryHandler<ExportSyncLogsQuery, string>
{
    public Task<Result<string>> Handle(ExportSyncLogsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success("Timestamp,Portal,Direction,Status\n2026-05-23,MockPortal,Inbound,Synced"));
}

public sealed class GetFailedSyncQueueQueryHandler : IQueryHandler<GetFailedSyncQueueQuery, List<SyncRecordSummaryDto>>
{
    public Task<Result<List<SyncRecordSummaryDto>>> Handle(GetFailedSyncQueueQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new List<SyncRecordSummaryDto>()));
}

public sealed class GetGovernmentAuditTrailQueryHandler : IQueryHandler<GetGovernmentAuditTrailQuery, List<GovernmentAuditEntryDto>>
{
    private readonly IGovernmentAuditRepository _repository;
    public GetGovernmentAuditTrailQueryHandler(IGovernmentAuditRepository repository) { _repository = repository; }
    public async Task<Result<List<GovernmentAuditEntryDto>>> Handle(GetGovernmentAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.QueryAsync(request.VerificationRequestId, cancellationToken);
        var dtos = entries.Select(e => new GovernmentAuditEntryDto(
            e.Id,
            e.VerificationRequestId,
            e.RegistryName,
            e.Direction.ToString(),
            e.QueryParameters,
            e.ResultCode,
            e.IntegrityHash,
            e.OccurredOnUtc)).ToList();
        return Result.Success(dtos);
    }
}

public sealed class GetApiVersionsQueryHandler : IQueryHandler<GetApiVersionsQuery, List<ApiVersionDto>>
{
    public Task<Result<List<ApiVersionDto>>> Handle(GetApiVersionsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new List<ApiVersionDto>
        {
            new ApiVersionDto("v1", "Active", DateTime.UtcNow.AddMonths(-12), null),
            new ApiVersionDto("v2", "Active", DateTime.UtcNow.AddMonths(-2), null)
        }));
}
