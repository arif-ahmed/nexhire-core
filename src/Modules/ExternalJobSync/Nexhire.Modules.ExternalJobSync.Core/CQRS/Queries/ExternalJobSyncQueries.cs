using Nexhire.Modules.ExternalJobSync.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.ExternalJobSync.Core.CQRS.Queries;

public sealed record GetSyncRecordByExternalRefQuery(
    string PortalName, 
    string ExternalJobId) : IQuery<SyncRecordSummaryDto?>;

public sealed record GetSubmissionLogsQuery(
    Guid PartnerId, 
    int Page = 1, 
    int PageSize = 20) : IQuery<List<SyncRecordSummaryDto>>;

public sealed record GetSubmissionLogDetailQuery(Guid LogId) : IQuery<SyncRecordDetailDto?>;

public sealed record GetPartnerSyncDashboardQuery(Guid PartnerId) : IQuery<PartnerSyncDashboardDto>;

public sealed record GetPartnerUsageStatsQuery(Guid PartnerId) : IQuery<PartnerUsageStatsDto>;

public sealed record GetIntegrationDashboardQuery() : IQuery<IntegrationDashboardDto>;

public sealed record GetConnectorSyncLogQuery(
    Guid ConnectorId, 
    int Page = 1, 
    int PageSize = 20) : IQuery<List<SyncRecordSummaryDto>>;

public sealed record ExportSyncLogsQuery() : IQuery<string>;

public sealed record GetFailedSyncQueueQuery() : IQuery<List<SyncRecordSummaryDto>>;

public sealed record GetSyncRecordDetailQuery(Guid SyncRecordId) : IQuery<SyncRecordDetailDto?>;

public sealed record GetVerificationStatusQuery(Guid VerificationRequestId) : IQuery<VerificationStatusDto?>;

public sealed record GetGovernmentAuditTrailQuery(Guid? VerificationRequestId = null) : IQuery<List<GovernmentAuditEntryDto>>;

public sealed record GetApiVersionsQuery() : IQuery<List<ApiVersionDto>>;
