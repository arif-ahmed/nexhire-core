using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;

public enum ConnectionStatus { Unverified, Connected, Failed }
public enum WebhookSigningAlgorithm { HmacSha256, BearerToken }

public sealed class ExternalConnector : AggregateRoot<Guid>
{
    private readonly List<WebhookSubscription> _webhookSubscriptions = new();

    public string PortalName { get; private set; } = null!;
    public string ApiEndpoint { get; private set; } = null!;
    public EncryptedCredentials Credentials { get; private set; } = null!;
    public ConnectionStatus ConnectionStatus { get; private set; }
    public SyncOptions SyncOptions { get; private set; } = null!;
    public IReadOnlyCollection<WebhookSubscription> WebhookSubscriptions => _webhookSubscriptions.AsReadOnly();
    public string SchemaVersion { get; private set; } = null!;
    public DateTime? LastPullOnUtc { get; private set; }
    public DateTime? LastPushOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private ExternalConnector() { }

    private ExternalConnector(
        Guid id, 
        string portalName, 
        string apiEndpoint, 
        EncryptedCredentials credentials, 
        string schemaVersion,
        DateTime createdOnUtc) : base(id)
    {
        PortalName = portalName;
        ApiEndpoint = apiEndpoint;
        Credentials = credentials;
        SchemaVersion = schemaVersion;
        ConnectionStatus = ConnectionStatus.Unverified;
        SyncOptions = SyncOptions.Create(PullInterval.Off, false, null);
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = createdOnUtc;
    }

    public static Result<ExternalConnector> Configure(
        string portalName, 
        string apiEndpoint, 
        EncryptedCredentials credentials, 
        string schemaVersion)
    {
        if (string.IsNullOrWhiteSpace(portalName))
            return Result.Failure<ExternalConnector>(new Error("Connector.PortalNameRequired", "Portal name is required."));
        
        if (string.IsNullOrWhiteSpace(apiEndpoint) || !Uri.TryCreate(apiEndpoint, UriKind.Absolute, out _))
            return Result.Failure<ExternalConnector>(new Error("Connector.InvalidEndpoint", "API base endpoint must be a valid absolute URL."));

        if (credentials == null)
            return Result.Failure<ExternalConnector>(new Error("Connector.CredentialsRequired", "Credentials are required."));

        if (string.IsNullOrWhiteSpace(schemaVersion))
            return Result.Failure<ExternalConnector>(new Error("Connector.SchemaVersionRequired", "Schema version is required."));

        return Result.Success(new ExternalConnector(
            Guid.NewGuid(), 
            portalName.Trim(), 
            apiEndpoint.Trim(), 
            credentials, 
            schemaVersion.Trim(), 
            DateTime.UtcNow));
    }

    public void MarkConnectionVerified()
    {
        ConnectionStatus = ConnectionStatus.Connected;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void MarkConnectionFailed()
    {
        ConnectionStatus = ConnectionStatus.Failed;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void UpdateCredentials(EncryptedCredentials credentials)
    {
        Credentials = credentials;
        ConnectionStatus = ConnectionStatus.Unverified;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void SetSyncOptions(SyncOptions syncOptions)
    {
        SyncOptions = syncOptions;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result AddWebhookSubscription(Guid subscriptionId, string callbackPath, EncryptedCredentials signingSecret, WebhookSigningAlgorithm algorithm)
    {
        if (string.IsNullOrWhiteSpace(callbackPath) || !callbackPath.StartsWith("/api/webhooks/external-portal/"))
            return Result.Failure(new Error("Webhook.InvalidPath", "Callback path must start with '/api/webhooks/external-portal/'."));

        if (signingSecret == null)
            return Result.Failure(new Error("Webhook.SigningSecretRequired", "Signing secret is required."));

        var sub = new WebhookSubscription(subscriptionId, callbackPath.Trim(), signingSecret, algorithm);
        _webhookSubscriptions.Add(sub);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result DisableWebhookSubscription(Guid subscriptionId)
    {
        var sub = _webhookSubscriptions.FirstOrDefault(s => s.Id == subscriptionId);
        if (sub == null)
            return Result.Failure(new Error("Webhook.NotFound", "Webhook subscription not found."));

        sub.Disable();
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void RecordPull()
    {
        LastPullOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void RecordPush()
    {
        LastPushOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

public sealed class WebhookSubscription : Entity<Guid>
{
    public string CallbackPath { get; private set; } = null!;
    public EncryptedCredentials SigningSecret { get; private set; } = null!;
    public WebhookSigningAlgorithm SigningAlgorithm { get; private set; }
    public bool IsEnabled { get; private set; }

    private WebhookSubscription() { }

    internal WebhookSubscription(Guid id, string callbackPath, EncryptedCredentials signingSecret, WebhookSigningAlgorithm signingAlgorithm) : base(id)
    {
        CallbackPath = callbackPath;
        SigningSecret = signingSecret;
        SigningAlgorithm = signingAlgorithm;
        IsEnabled = true;
    }

    internal void Disable()
    {
        IsEnabled = false;
    }
}
