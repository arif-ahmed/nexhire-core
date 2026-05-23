using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Partner?> GetByApiKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task AddAsync(Partner partner, CancellationToken cancellationToken = default);
    void Update(Partner partner);
}

public interface IExternalConnectorRepository
{
    Task<ExternalConnector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExternalConnector>> ListDueForPullAsync(CancellationToken cancellationToken = default);
    Task<List<ExternalConnector>> ListWithPushOnPublishAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ExternalConnector connector, CancellationToken cancellationToken = default);
    void Update(ExternalConnector connector);
}

public interface IMappingProfileRepository
{
    Task<MappingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MappingProfile?> GetActiveAsync(string portalName, string schemaVersion, MappingDirection direction, CancellationToken cancellationToken = default);
    Task AddAsync(MappingProfile profile, CancellationToken cancellationToken = default);
    void Update(MappingProfile profile);
}

public interface ISyncRecordRepository
{
    Task<SyncRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SyncRecord?> GetByExternalRefAsync(ExternalRef externalRef, CancellationToken cancellationToken = default);
    Task<List<SyncRecord>> ListByStatusAsync(SyncStatus status, CancellationToken cancellationToken = default);
    Task<List<SyncRecord>> ListByConnectorAsync(Guid connectorId, CancellationToken cancellationToken = default);
    Task AddAsync(SyncRecord record, CancellationToken cancellationToken = default);
    void Update(SyncRecord record);
}

public interface IVerificationRequestRepository
{
    Task<VerificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VerificationRequest?> GetLatestForSubjectAsync(Guid subjectId, VerificationKind kind, CancellationToken cancellationToken = default);
    Task AddAsync(VerificationRequest request, CancellationToken cancellationToken = default);
    void Update(VerificationRequest request);
}

public interface IGovernmentAuditRepository
{
    Task AppendAsync(GovernmentAuditEntry entry, CancellationToken cancellationToken = default);
    Task<string?> GetLastEntryHashAsync(CancellationToken cancellationToken = default);
    Task<List<GovernmentAuditEntry>> QueryAsync(Guid? verificationRequestId, CancellationToken cancellationToken = default);
}

public interface IApiVersionRegistryRepository
{
    Task<ApiVersionRegistry> GetSingletonAsync(CancellationToken cancellationToken = default);
    void Update(ApiVersionRegistry registry);
}

public interface IExternalJobSyncUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
