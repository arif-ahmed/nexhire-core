using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Services;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.CQRS.Commands;

public sealed class RegisterPartnerCommandHandler : ICommandHandler<RegisterPartnerCommand, Guid>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public RegisterPartnerCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterPartnerCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.ContactEmail);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        var partnerResult = Partner.Register(request.Name, emailResult.Value, request.Website, request.CompanyInfo);
        if (partnerResult.IsFailure)
            return Result.Failure<Guid>(partnerResult.Error);

        await _partnerRepository.AddAsync(partnerResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(partnerResult.Value.Id);
    }
}

public sealed class ApprovePartnerCommandHandler : ICommandHandler<ApprovePartnerCommand>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public ApprovePartnerCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApprovePartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure(new Error("Partner.NotFound", "Partner not found."));

        var approveResult = partner.Approve();
        if (approveResult.IsFailure)
            return approveResult;

        // Auto-generate primary API key on approval
        var keyGen = new ApiKeyGenerator();
        var (plaintext, hash, prefix) = keyGen.Generate();
        
        var keyResult = partner.IssueApiKey(Guid.NewGuid(), hash, prefix);
        if (keyResult.IsFailure)
            return keyResult;

        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // In a real application, the plaintext key is delivered out-of-band (e.g. email/web UI).
        // Since we are returning Result, we could log or expose it via IssueApiKeyCommand.
        return Result.Success();
    }
}

public sealed class IssueApiKeyCommandHandler : ICommandHandler<IssueApiKeyCommand, string>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public IssueApiKeyCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(IssueApiKeyCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure<string>(new Error("Partner.NotFound", "Partner not found."));

        var keyGen = new ApiKeyGenerator();
        var (plaintext, hash, prefix) = keyGen.Generate();

        var result = partner.IssueApiKey(Guid.NewGuid(), hash, prefix, request.ExpiresOnUtc);
        if (result.IsFailure)
            return Result.Failure<string>(result.Error);

        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plaintext);
    }
}

public sealed class RegenerateApiKeyCommandHandler : ICommandHandler<RegenerateApiKeyCommand, string>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public RegenerateApiKeyCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(RegenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure<string>(new Error("Partner.NotFound", "Partner not found."));

        var keyGen = new ApiKeyGenerator();
        var (plaintext, hash, prefix) = keyGen.Generate();

        var result = partner.RegenerateApiKey(Guid.NewGuid(), hash, prefix, request.ExpiresOnUtc);
        if (result.IsFailure)
            return Result.Failure<string>(result.Error);

        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plaintext);
    }
}

public sealed class RevokeApiKeyCommandHandler : ICommandHandler<RevokeApiKeyCommand>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public RevokeApiKeyCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure(new Error("Partner.NotFound", "Partner not found."));

        var result = partner.RevokeApiKey(request.ApiKeyId);
        if (result.IsFailure)
            return result;

        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SetPartnerIpWhitelistCommandHandler : ICommandHandler<SetPartnerIpWhitelistCommand>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public SetPartnerIpWhitelistCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetPartnerIpWhitelistCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure(new Error("Partner.NotFound", "Partner not found."));

        var result = partner.SetIpWhitelist(request.Ips);
        if (result.IsFailure)
            return result;

        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SetPartnerRateLimitCommandHandler : ICommandHandler<SetPartnerRateLimitCommand>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public SetPartnerRateLimitCommandHandler(IPartnerRepository partnerRepository, IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetPartnerRateLimitCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partnerRepository.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner == null)
            return Result.Failure(new Error("Partner.NotFound", "Partner not found."));

        var rateLimitResult = RateLimit.Create(request.MaxRequests, request.Window);
        if (rateLimitResult.IsFailure)
            return Result.Failure(rateLimitResult.Error);

        partner.SetRateLimit(rateLimitResult.Value);
        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class ConfigureExternalConnectorCommandHandler : ICommandHandler<ConfigureExternalConnectorCommand, Guid>
{
    private readonly IExternalConnectorRepository _connectorRepository;
    private readonly ICredentialEncryptionPort _encryptionPort;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public ConfigureExternalConnectorCommandHandler(
        IExternalConnectorRepository connectorRepository, 
        ICredentialEncryptionPort encryptionPort, 
        IExternalJobSyncUnitOfWork unitOfWork)
    {
        _connectorRepository = connectorRepository;
        _encryptionPort = encryptionPort;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ConfigureExternalConnectorCommand request, CancellationToken cancellationToken)
    {
        var encResult = _encryptionPort.Encrypt(request.PlainTextClientSecret);
        if (encResult.IsFailure)
            return Result.Failure<Guid>(encResult.Error);

        var connResult = ExternalConnector.Configure(request.PortalName, request.ApiEndpoint, encResult.Value, request.SchemaVersion);
        if (connResult.IsFailure)
            return Result.Failure<Guid>(connResult.Error);

        await _connectorRepository.AddAsync(connResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(connResult.Value.Id);
    }
}

public sealed class PushJobViaApiCommandHandler : ICommandHandler<PushJobViaApiCommand, Guid>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly ISyncRecordRepository _syncRecordRepository;
    private readonly IMappingProfileRepository _mappingProfileRepository;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IGeocodingPort _geocodingPort;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public PushJobViaApiCommandHandler(
        IPartnerRepository partnerRepository, 
        ISyncRecordRepository syncRecordRepository, 
        IMappingProfileRepository mappingProfileRepository, 
        ITaxonomyApi taxonomyApi, 
        IGeocodingPort geocodingPort, 
        IExternalJobSyncUnitOfWork unitOfWork)
    {
        _partnerRepository = partnerRepository;
        _syncRecordRepository = syncRecordRepository;
        _mappingProfileRepository = mappingProfileRepository;
        _taxonomyApi = taxonomyApi;
        _geocodingPort = geocodingPort;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(PushJobViaApiCommand request, CancellationToken cancellationToken)
    {
        // Compute key hash from incoming plain key
        var keyBytes = Encoding.UTF8.GetBytes(request.ApiKey);
        var hashBytes = SHA256.HashData(keyBytes);
        var keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var partner = await _partnerRepository.GetByApiKeyHashAsync(keyHash, cancellationToken);
        if (partner == null || partner.Status != PartnerStatus.Active)
            return Result.Failure<Guid>(new Error("E-API-UNAUTHORIZED", "Invalid or inactive partner API key."));

        var key = partner.ApiKeys.First(k => k.KeyHash == keyHash);
        if (key.Status != ApiKeyStatus.Active || (key.ExpiresOnUtc != null && key.ExpiresOnUtc < DateTime.UtcNow))
            return Result.Failure<Guid>(new Error("E-API-KEY-EXPIRED", "API key has expired."));

        // Detect schema version
        var knownVersions = new List<string> { "v1", "v2" };
        var detector = new SchemaVersionDetector();
        var versionResult = detector.Detect(request.RawPayload, knownVersions);
        if (versionResult.IsFailure)
            return Result.Failure<Guid>(versionResult.Error);

        // Fetch Mapping Profile
        var profile = await _mappingProfileRepository.GetActiveAsync(partner.Name, versionResult.Value, MappingDirection.Inbound, cancellationToken);
        if (profile == null)
            return Result.Failure<Guid>(new Error("E-SYNC-NO-PROFILE", "Active inbound field mapping profile not found."));

        // Process parsing using JobDataTransformer
        var transformer = new JobDataTransformer();
        var normalResult = await transformer.ToNormalisedAsync(
            request.RawPayload, 
            profile, 
            raw => _taxonomyApi.MapSkillToTaxonomyCodeAsync(raw, cancellationToken), 
            raw => _geocodingPort.NormaliseAsync(raw, cancellationToken));

        if (normalResult.IsFailure && normalResult.Error.Code == "E-SYNC-MISSING-FIELD")
        {
            // Missing required fields => Quarantine SyncRecord
            var externalRef = ExternalRef.Create(partner.Name, Guid.NewGuid().ToString()).Value; // dummy ref for quarantine tracing
            var recordResult = SyncRecord.StartInbound(externalRef, request.RawPayload, partnerId: partner.Id);
            if (recordResult.IsFailure)
                return Result.Failure<Guid>(recordResult.Error);

            var record = recordResult.Value;
            record.Quarantine("E-SYNC-MISSING-FIELD", normalResult.Error.Message);
            await _syncRecordRepository.AddAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<Guid>(new Error("E-API-VALIDATION-ERROR", normalResult.Error.Message));
        }

        if (normalResult.IsFailure)
            return Result.Failure<Guid>(normalResult.Error);

        var posting = normalResult.Value;

        // Duplicate Check
        var exactRefExists = new Func<ExternalRef, Task<bool>>(async r => 
            await _syncRecordRepository.GetByExternalRefAsync(r, cancellationToken) != null);
        var fuzzyMatchExists = new Func<string, string, string, Task<bool>>((t, c, l) => Task.FromResult(false)); // skip fuzzy in pushes

        var duplicateDetector = new DuplicateJobDetector();
        if (duplicateDetector.IsDuplicate(posting.ExternalRef, posting, exactRefExists, fuzzyMatchExists))
            return Result.Failure<Guid>(new Error("E-API-DUPLICATE-JOB", "Job posting with this external reference already exists."));

        // Create SyncRecord in Accepted status
        var syncResult = SyncRecord.StartInbound(posting.ExternalRef, request.RawPayload, partnerId: partner.Id);
        if (syncResult.IsFailure)
            return Result.Failure<Guid>(syncResult.Error);

        var syncRecord = syncResult.Value;
        syncRecord.RecordNormalised(posting);
        syncRecord.MarkSynced(Guid.NewGuid()); // Mirrored internal job ID is generated mockingly here

        await _syncRecordRepository.AddAsync(syncRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(syncRecord.Id);
    }
}

public sealed class IngestExternalJobsCommandHandler : ICommandHandler<IngestExternalJobsCommand, int>
{
    private readonly IExternalConnectorRepository _connectorRepository;
    private readonly ISyncRecordRepository _syncRecordRepository;
    private readonly IMappingProfileRepository _mappingProfileRepository;
    private readonly IExternalPortalPort _portalPort;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IGeocodingPort _geocodingPort;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public IngestExternalJobsCommandHandler(
        IExternalConnectorRepository connectorRepository, 
        ISyncRecordRepository syncRecordRepository, 
        IMappingProfileRepository mappingProfileRepository, 
        IExternalPortalPort portalPort, 
        ITaxonomyApi taxonomyApi, 
        IGeocodingPort geocodingPort, 
        IExternalJobSyncUnitOfWork unitOfWork)
    {
        _connectorRepository = connectorRepository;
        _syncRecordRepository = syncRecordRepository;
        _mappingProfileRepository = mappingProfileRepository;
        _portalPort = portalPort;
        _taxonomyApi = taxonomyApi;
        _geocodingPort = geocodingPort;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(IngestExternalJobsCommand request, CancellationToken cancellationToken)
    {
        var connector = await _connectorRepository.GetByIdAsync(request.ConnectorId, cancellationToken);
        if (connector == null)
            return Result.Failure<int>(new Error("Connector.NotFound", "Connector not found."));

        var profile = await _mappingProfileRepository.GetActiveAsync(connector.PortalName, connector.SchemaVersion, MappingDirection.Inbound, cancellationToken);
        if (profile == null)
            return Result.Failure<int>(new Error("E-SYNC-NO-PROFILE", "Active field mapping profile not found."));

        var fetchResult = await _portalPort.FetchJobsAsync(connector.ApiEndpoint, connector.Credentials, connector.LastPullOnUtc, cancellationToken);
        if (fetchResult.IsFailure)
        {
            connector.MarkConnectionFailed();
            _connectorRepository.Update(connector);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<int>(fetchResult.Error);
        }

        connector.MarkConnectionVerified();
        connector.RecordPull();
        _connectorRepository.Update(connector);

        int count = 0;
        var transformer = new JobDataTransformer();

        foreach (var rawPayload in fetchResult.Value)
        {
            var normalResult = await transformer.ToNormalisedAsync(
                rawPayload, 
                profile, 
                raw => _taxonomyApi.MapSkillToTaxonomyCodeAsync(raw, cancellationToken), 
                raw => _geocodingPort.NormaliseAsync(raw, cancellationToken));

            if (normalResult.IsFailure && normalResult.Error.Code == "E-SYNC-MISSING-FIELD")
            {
                // Incomplete job => Quarantine
                var externalRef = ExternalRef.Create(connector.PortalName, Guid.NewGuid().ToString()).Value;
                var record = SyncRecord.StartInbound(externalRef, rawPayload, connectorId: connector.Id).Value;
                record.Quarantine("E-SYNC-MISSING-FIELD", normalResult.Error.Message);
                await _syncRecordRepository.AddAsync(record, cancellationToken);
                continue;
            }

            if (normalResult.IsFailure)
                continue;

            var posting = normalResult.Value;

            // Deduplicate
            var existingRecord = await _syncRecordRepository.GetByExternalRefAsync(posting.ExternalRef, cancellationToken);
            if (existingRecord != null)
            {
                // Update existing record
                existingRecord.Retry();
                existingRecord.RecordNormalised(posting);
                existingRecord.MarkSynced(existingRecord.InternalJobId);
                _syncRecordRepository.Update(existingRecord);
            }
            else
            {
                // Create new record
                var newRecord = SyncRecord.StartInbound(posting.ExternalRef, rawPayload, connectorId: connector.Id).Value;
                newRecord.RecordNormalised(posting);
                newRecord.MarkSynced(Guid.NewGuid());
                await _syncRecordRepository.AddAsync(newRecord, cancellationToken);
            }

            count++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(count);
    }
}

public sealed class VerifyIdentityViaGovernmentCommandHandler : ICommandHandler<VerifyIdentityViaGovernmentCommand, VerificationResult>
{
    private readonly IVerificationRequestRepository _verificationRepository;
    private readonly IGovernmentRegistryPort _registryPort;
    private readonly IGovernmentAuditRepository _auditRepository;
    private readonly IExternalJobSyncUnitOfWork _unitOfWork;

    public VerifyIdentityViaGovernmentCommandHandler(
        IVerificationRequestRepository verificationRepository, 
        IGovernmentRegistryPort registryPort, 
        IGovernmentAuditRepository auditRepository, 
        IExternalJobSyncUnitOfWork unitOfWork)
    {
        _verificationRepository = verificationRepository;
        _registryPort = registryPort;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VerificationResult>> Handle(VerifyIdentityViaGovernmentCommand request, CancellationToken cancellationToken)
    {
        // Enforce Cache Check: Fetch latest verification first
        var latest = await _verificationRepository.GetLatestForSubjectAsync(request.UserId, VerificationKind.Identity, cancellationToken);
        if (latest != null && latest.IsCacheValid(DateTime.UtcNow) && latest.Result != null)
        {
            return Result.Success(latest.Result);
        }

        var startResult = VerificationRequest.StartIdentity(request.UserId, request.Registry, request.Consent, request.Payload);
        if (startResult.IsFailure)
            return Result.Failure<VerificationResult>(startResult.Error);

        var verifyRequest = startResult.Value;
        verifyRequest.BeginProcessing();
        await _verificationRepository.AddAsync(verifyRequest, cancellationToken);

        // Audit Trail: Append Query
        var prevHash = await _auditRepository.GetLastEntryHashAsync(cancellationToken) ?? "";
        var queryParams = JsonSerializer.Serialize(request.Payload.Fields);
        var auditQuery = GovernmentAuditEntry.Record(
            verifyRequest.Id, 
            request.UserId, 
            request.Registry.Name, 
            AuditDirection.Query, 
            queryParams, 
            "PENDING", 
            0, 
            "IdentityCheckInitiated", 
            $"ConsentVersion:{request.Consent.ConsentVersion}", 
            prevHash).Value;

        await _auditRepository.AppendAsync(auditQuery, cancellationToken);

        // Call Government Registry Port
        var callResult = await _registryPort.VerifyAsync(request.Registry, VerificationKind.Identity, request.Payload, cancellationToken);
        
        // Audit Trail: Append Response
        var prevHashResp = auditQuery.IntegrityHash;
        var resultCode = callResult.IsSuccess ? "200" : "500";
        var auditResp = GovernmentAuditEntry.Record(
            verifyRequest.Id, 
            request.UserId, 
            request.Registry.Name, 
            AuditDirection.Response, 
            JsonSerializer.Serialize(callResult), 
            resultCode, 
            100, 
            "IdentityCheckOutcomeProcessed", 
            $"ConsentVersion:{request.Consent.ConsentVersion}", 
            prevHashResp).Value;

        await _auditRepository.AppendAsync(auditResp, cancellationToken);

        if (callResult.IsFailure)
        {
            verifyRequest.RecordError(callResult.Error.Message);
            _verificationRepository.Update(verifyRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<VerificationResult>(callResult.Error);
        }

        var outcome = callResult.Value;
        if (outcome.Outcome == VerificationOutcome.Match)
        {
            verifyRequest.RecordVerified(outcome);
        }
        else
        {
            verifyRequest.RecordUnverified(outcome);
        }

        _verificationRepository.Update(verifyRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(outcome);
    }
}

// Stubs for remaining commands to ensure perfect modular compilation
public sealed class ExportJobCommandHandler : ICommandHandler<ExportJobCommand>
{
    public Task<Result> Handle(ExportJobCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed class RetrySyncRecordCommandHandler : ICommandHandler<RetrySyncRecordCommand>
{
    private readonly ISyncRecordRepository _repository;
    private readonly IExternalJobSyncUnitOfWork _uow;
    public RetrySyncRecordCommandHandler(ISyncRecordRepository repository, IExternalJobSyncUnitOfWork uow) { _repository = repository; _uow = uow; }
    public async Task<Result> Handle(RetrySyncRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.SyncRecordId, cancellationToken);
        if (record == null) return Result.Failure(new Error("SyncRecord.NotFound", "Not found."));
        var res = record.Retry();
        if (res.IsFailure) return res;
        _repository.Update(record);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ManualOverrideSyncRecordCommandHandler : ICommandHandler<ManualOverrideSyncRecordCommand>
{
    private readonly ISyncRecordRepository _repository;
    private readonly IExternalJobSyncUnitOfWork _uow;
    public ManualOverrideSyncRecordCommandHandler(ISyncRecordRepository repository, IExternalJobSyncUnitOfWork uow) { _repository = repository; _uow = uow; }
    public async Task<Result> Handle(ManualOverrideSyncRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.SyncRecordId, cancellationToken);
        if (record == null) return Result.Failure(new Error("SyncRecord.NotFound", "Not found."));
        var res = record.ManualOverride(request.CorrectedPayload, request.EngineerId);
        if (res.IsFailure) return res;
        _repository.Update(record);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class VerifyEducationalCredentialCommandHandler : ICommandHandler<VerifyEducationalCredentialCommand, VerificationResult>
{
    public Task<Result<VerificationResult>> Handle(VerifyEducationalCredentialCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(VerificationResult.Create(VerificationOutcome.Match, "DEG-1029", request.Registry.Name, DateTime.UtcNow).Value));
}

public sealed class RecordConsentDecisionCommandHandler : ICommandHandler<RecordConsentDecisionCommand>
{
    public Task<Result> Handle(RecordConsentDecisionCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed class DeleteGovernmentDataForUserCommandHandler : ICommandHandler<DeleteGovernmentDataForUserCommand>
{
    public Task<Result> Handle(DeleteGovernmentDataForUserCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed class DeprecateApiVersionCommandHandler : ICommandHandler<DeprecateApiVersionCommand>
{
    public Task<Result> Handle(DeprecateApiVersionCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
