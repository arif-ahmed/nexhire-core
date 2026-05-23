using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.ExternalJobSync.Core.CQRS.Commands;

public sealed record RegisterPartnerCommand(
    string Name, 
    string ContactEmail, 
    string? Website, 
    string? CompanyInfo) : ICommand<Guid>;

public sealed record ApprovePartnerCommand(Guid PartnerId) : ICommand;

public sealed record IssueApiKeyCommand(Guid PartnerId, DateTime? ExpiresOnUtc = null) : ICommand<string>;

public sealed record RegenerateApiKeyCommand(Guid PartnerId, DateTime? ExpiresOnUtc = null) : ICommand<string>;

public sealed record RevokeApiKeyCommand(Guid PartnerId, Guid ApiKeyId) : ICommand;

public sealed record SetPartnerIpWhitelistCommand(Guid PartnerId, List<string> Ips) : ICommand;

public sealed record SetPartnerRateLimitCommand(Guid PartnerId, int MaxRequests, RateWindow Window) : ICommand;

public sealed record ConfigureExternalConnectorCommand(
    string PortalName, 
    string ApiEndpoint, 
    string PlainTextClientSecret, 
    string SchemaVersion) : ICommand<Guid>;

public sealed record RotateConnectorCredentialsCommand(Guid ConnectorId, string PlainTextClientSecret) : ICommand;

public sealed record SetConnectorSyncOptionsCommand(
    Guid ConnectorId, 
    PullInterval PullInterval, 
    bool PushOnPublish, 
    Guid? MappingProfileId) : ICommand;

public sealed record AddWebhookSubscriptionCommand(
    Guid ConnectorId, 
    string CallbackPath, 
    string PlainTextSigningSecret, 
    WebhookSigningAlgorithm Algorithm) : ICommand<Guid>;

public sealed record DisableWebhookSubscriptionCommand(Guid ConnectorId, Guid SubscriptionId) : ICommand;

public sealed record ConfigureMappingProfileCommand(
    string PortalName, 
    string SchemaVersion, 
    MappingDirection Direction) : ICommand<Guid>;

public sealed record AddFieldMappingCommand(
    Guid MappingProfileId, 
    string SourcePath, 
    string TargetPath, 
    TransformKind TransformKind, 
    string? TransformArgs, 
    bool IsRequired) : ICommand<Guid>;

public sealed record ActivateMappingProfileCommand(Guid MappingProfileId) : ICommand;

public sealed record PushJobViaApiCommand(string ApiKey, string RawPayload) : ICommand<Guid>;

public sealed record IngestExternalJobsCommand(Guid ConnectorId) : ICommand<int>;

public sealed record ExportJobCommand(Guid JobPostingId, Guid ConnectorId) : ICommand;

public sealed record RetrySyncRecordCommand(Guid SyncRecordId) : ICommand;

public sealed record ManualOverrideSyncRecordCommand(
    Guid SyncRecordId, 
    NormalisedJobPosting CorrectedPayload, 
    Guid EngineerId) : ICommand;

public sealed record VerifyIdentityViaGovernmentCommand(
    Guid UserId, 
    Registry Registry, 
    ConsentRecord Consent, 
    MinimisedRequestPayload Payload) : ICommand<VerificationResult>;

public sealed record VerifyEducationalCredentialCommand(
    Guid SeekerProfileId, 
    Guid UserId, 
    Registry Registry, 
    ConsentRecord Consent, 
    MinimisedRequestPayload Payload) : ICommand<VerificationResult>;

public sealed record RecordConsentDecisionCommand(
    Guid SeekerUserId, 
    bool Granted, 
    string Version) : ICommand;

public sealed record DeleteGovernmentDataForUserCommand(Guid SeekerUserId) : ICommand;

public sealed record DeprecateApiVersionCommand(
    string Version, 
    DateTime SunsetOnUtc, 
    string? MigrationGuideUrl) : ICommand;
