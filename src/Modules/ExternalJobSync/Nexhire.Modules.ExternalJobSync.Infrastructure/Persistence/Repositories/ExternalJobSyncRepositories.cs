using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Persistence.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public PartnerRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Partners
            .Include(p => p.ApiKeys)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Partner?> GetByApiKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Partners
            .Include(p => p.ApiKeys)
            .FirstOrDefaultAsync(p => p.ApiKeys.Any(k => k.KeyHash == keyHash), cancellationToken);
    }

    public async Task AddAsync(Partner partner, CancellationToken cancellationToken = default)
    {
        await _dbContext.Partners.AddAsync(partner, cancellationToken);
    }

    public void Update(Partner partner)
    {
        _dbContext.Partners.Update(partner);
    }
}

public sealed class ExternalConnectorRepository : IExternalConnectorRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public ExternalConnectorRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExternalConnector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Connectors
            .Include(c => c.WebhookSubscriptions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<ExternalConnector>> ListDueForPullAsync(CancellationToken cancellationToken = default)
    {
        // Simple logic: returns connectors configured to pull that have not been pulled recently
        return await _dbContext.Connectors
            .Where(c => c.SyncOptions.PullInterval != PullInterval.Off)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExternalConnector>> ListWithPushOnPublishAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Connectors
            .Where(c => c.SyncOptions.PushOnPublish)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExternalConnector connector, CancellationToken cancellationToken = default)
    {
        await _dbContext.Connectors.AddAsync(connector, cancellationToken);
    }

    public void Update(ExternalConnector connector)
    {
        _dbContext.Connectors.Update(connector);
    }
}

public sealed class MappingProfileRepository : IMappingProfileRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public MappingProfileRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MappingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MappingProfiles
            .Include(m => m.FieldMappings)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<MappingProfile?> GetActiveAsync(string portalName, string schemaVersion, MappingDirection direction, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MappingProfiles
            .Include(m => m.FieldMappings)
            .FirstOrDefaultAsync(m => 
                m.PortalName == portalName && 
                m.SchemaVersion == schemaVersion && 
                m.Direction == direction && 
                m.IsActive, 
                cancellationToken);
    }

    public async Task AddAsync(MappingProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.MappingProfiles.AddAsync(profile, cancellationToken);
    }

    public void Update(MappingProfile profile)
    {
        _dbContext.MappingProfiles.Update(profile);
    }
}

public sealed class SyncRecordRepository : ISyncRecordRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public SyncRecordRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SyncRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncRecords
            .Include(s => s.Attempts)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SyncRecord?> GetByExternalRefAsync(ExternalRef externalRef, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncRecords
            .Include(s => s.Attempts)
            .FirstOrDefaultAsync(s => 
                s.ExternalRef.PortalName == externalRef.PortalName && 
                s.ExternalRef.ExternalJobId == externalRef.ExternalJobId, 
                cancellationToken);
    }

    public async Task<List<SyncRecord>> ListByStatusAsync(SyncStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncRecords
            .Include(s => s.Attempts)
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SyncRecord>> ListByConnectorAsync(Guid connectorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncRecords
            .Include(s => s.Attempts)
            .Where(s => s.ConnectorId == connectorId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SyncRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncRecords.AddAsync(record, cancellationToken);
    }

    public void Update(SyncRecord record)
    {
        _dbContext.SyncRecords.Update(record);
    }
}

public sealed class VerificationRequestRepository : IVerificationRequestRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public VerificationRequestRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VerificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VerificationRequests.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<VerificationRequest?> GetLatestForSubjectAsync(Guid subjectId, VerificationKind kind, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VerificationRequests
            .Where(v => (v.SubjectUserId == subjectId || v.SubjectJobSeekerProfileId == subjectId || v.SubjectEmployerId == subjectId) && v.Kind == kind)
            .OrderByDescending(v => v.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(VerificationRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.VerificationRequests.AddAsync(request, cancellationToken);
    }

    public void Update(VerificationRequest request)
    {
        _dbContext.VerificationRequests.Update(request);
    }
}

public sealed class GovernmentAuditRepository : IGovernmentAuditRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public GovernmentAuditRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AppendAsync(GovernmentAuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.GovernmentAuditEntries.AddAsync(entry, cancellationToken);
    }

    public async Task<string?> GetLastEntryHashAsync(CancellationToken cancellationToken = default)
    {
        var last = await _dbContext.GovernmentAuditEntries
            .OrderByDescending(e => e.OccurredOnUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return last?.IntegrityHash;
    }

    public async Task<List<GovernmentAuditEntry>> QueryAsync(Guid? verificationRequestId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GovernmentAuditEntries.AsQueryable();
        if (verificationRequestId != null)
        {
            query = query.Where(e => e.VerificationRequestId == verificationRequestId);
        }
        return await query.OrderBy(e => e.OccurredOnUtc).ToListAsync(cancellationToken);
    }
}

public sealed class ApiVersionRegistryRepository : IApiVersionRegistryRepository
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public ApiVersionRegistryRepository(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiVersionRegistry> GetSingletonAsync(CancellationToken cancellationToken = default)
    {
        var registry = await _dbContext.ApiVersionRegistries
            .Include(r => r.Versions)
            .FirstOrDefaultAsync(r => r.Id == ApiVersionRegistry.SingletonId, cancellationToken);

        if (registry == null)
        {
            registry = ApiVersionRegistry.Create();
            await _dbContext.ApiVersionRegistries.AddAsync(registry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return registry;
    }

    public void Update(ApiVersionRegistry registry)
    {
        _dbContext.ApiVersionRegistries.Update(registry);
    }
}

public sealed class ExternalJobSyncUnitOfWork : IExternalJobSyncUnitOfWork
{
    private readonly ExternalJobSyncDbContext _dbContext;

    public ExternalJobSyncUnitOfWork(ExternalJobSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
