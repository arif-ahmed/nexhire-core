using Nexhire.Modules.ExternalJobSync.Core.Domain.Events;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;

public enum PartnerStatus { PendingActivation, Active, Suspended, Revoked }
public enum ApiKeyStatus { Active, Revoked, Expired }

public sealed class Partner : AggregateRoot<Guid>
{
    private readonly List<ApiKey> _apiKeys = new();
    private readonly List<string> _ipWhitelist = new();

    public string Name { get; private set; } = null!;
    public EmailAddress ContactEmail { get; private set; } = null!;
    public string? Website { get; private set; }
    public string? CompanyInfo { get; private set; }
    public PartnerStatus Status { get; private set; }
    public IReadOnlyCollection<ApiKey> ApiKeys => _apiKeys.AsReadOnly();
    public IReadOnlyCollection<string> IpWhitelist => _ipWhitelist.AsReadOnly();
    public RateLimit? RateLimit { get; private set; }
    public bool PublicAttribution { get; private set; }
    public DateTime RegisteredOnUtc { get; private set; }
    public DateTime? ActivatedOnUtc { get; private set; }

    private Partner() { }

    private Partner(Guid id, string name, EmailAddress contactEmail, string? website, string? companyInfo, DateTime registeredOnUtc) : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
        Website = website;
        CompanyInfo = companyInfo;
        Status = PartnerStatus.PendingActivation;
        PublicAttribution = false;
        RegisteredOnUtc = registeredOnUtc;
    }

    public static Result<Partner> Register(string name, EmailAddress contactEmail, string? website, string? companyInfo)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Partner>(new Error("Partner.NameRequired", "Partner name is required."));
        if (contactEmail == null)
            return Result.Failure<Partner>(new Error("Partner.EmailRequired", "Contact email is required."));

        var partner = new Partner(Guid.NewGuid(), name.Trim(), contactEmail, website?.Trim(), companyInfo?.Trim(), DateTime.UtcNow);
        partner.RaiseDomainEvent(new PartnerRegistered(partner.Id, partner.Name, partner.ContactEmail.Value));
        return Result.Success(partner);
    }

    public Result Approve()
    {
        if (Status != PartnerStatus.PendingActivation)
            return Result.Failure(new Error("Partner.NotPending", "Only pending partners can be approved."));

        Status = PartnerStatus.Active;
        ActivatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PartnerActivated(Id));
        return Result.Success();
    }

    public Result IssueApiKey(Guid apiKeyId, string keyHash, string keyPrefix, DateTime? expiresOnUtc = null)
    {
        if (Status != PartnerStatus.Active)
            return Result.Failure(new Error("Partner.NotActive", "API keys can only be issued to active partners."));

        // Revoke all existing active keys
        foreach (var key in _apiKeys.Where(k => k.Status == ApiKeyStatus.Active))
        {
            key.Revoke();
            RaiseDomainEvent(new ApiKeyRevoked(Id, key.Id));
        }

        var newKey = new ApiKey(apiKeyId, keyHash, keyPrefix, expiresOnUtc);
        _apiKeys.Add(newKey);
        RaiseDomainEvent(new ApiKeyIssued(Id, newKey.Id, newKey.KeyPrefix));
        return Result.Success();
    }

    public Result RegenerateApiKey(Guid apiKeyId, string keyHash, string keyPrefix, DateTime? expiresOnUtc = null)
    {
        return IssueApiKey(apiKeyId, keyHash, keyPrefix, expiresOnUtc);
    }

    public Result RevokeApiKey(Guid apiKeyId)
    {
        var key = _apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (key == null)
            return Result.Failure(new Error("Partner.KeyNotFound", "API key not found."));

        if (key.Status != ApiKeyStatus.Active)
            return Result.Failure(new Error("Partner.KeyNotActive", "API key is not active."));

        key.Revoke();
        RaiseDomainEvent(new ApiKeyRevoked(Id, key.Id));
        return Result.Success();
    }

    public Result ExpireApiKey(Guid apiKeyId)
    {
        var key = _apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (key == null)
            return Result.Failure(new Error("Partner.KeyNotFound", "API key not found."));

        key.Expire();
        return Result.Success();
    }

    public Result SetIpWhitelist(List<string> ips)
    {
        if (ips == null)
            return Result.Failure(new Error("Partner.IpWhitelistNull", "IP whitelist cannot be null."));

        _ipWhitelist.Clear();
        foreach (var ip in ips)
        {
            var trimmedIp = ip.Trim();
            if (string.IsNullOrEmpty(trimmedIp))
                continue;
            
            // Simple validation (IPv4 or IPv6 format check)
            if (!trimmedIp.Contains(".") && !trimmedIp.Contains(":"))
                return Result.Failure(new Error("Partner.InvalidIp", $"IP address '{trimmedIp}' is invalid."));

            _ipWhitelist.Add(trimmedIp);
        }

        return Result.Success();
    }

    public void SetRateLimit(RateLimit? rateLimit)
    {
        RateLimit = rateLimit;
    }

    public void SetPublicAttribution(bool enabled)
    {
        PublicAttribution = enabled;
    }

    public Result Suspend()
    {
        if (Status == PartnerStatus.Revoked)
            return Result.Failure(new Error("Partner.CannotModify", "Cannot suspend a revoked partner."));

        var oldStatus = Status.ToString();
        Status = PartnerStatus.Suspended;
        RaiseDomainEvent(new PartnerStatusChanged(Id, oldStatus, Status.ToString()));
        return Result.Success();
    }

    public Result Revoke()
    {
        var oldStatus = Status.ToString();
        Status = PartnerStatus.Revoked;

        // Revoke all API keys
        foreach (var key in _apiKeys.Where(k => k.Status == ApiKeyStatus.Active))
        {
            key.Revoke();
            RaiseDomainEvent(new ApiKeyRevoked(Id, key.Id));
        }

        RaiseDomainEvent(new PartnerStatusChanged(Id, oldStatus, Status.ToString()));
        return Result.Success();
    }
}

public sealed class ApiKey : Entity<Guid>
{
    public string KeyHash { get; private set; } = null!;
    public string KeyPrefix { get; private set; } = null!;
    public ApiKeyStatus Status { get; private set; }
    public DateTime? ExpiresOnUtc { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    private ApiKey() { }

    internal ApiKey(Guid id, string keyHash, string keyPrefix, DateTime? expiresOnUtc = null) : base(id)
    {
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        Status = ApiKeyStatus.Active;
        ExpiresOnUtc = expiresOnUtc;
        IssuedOnUtc = DateTime.UtcNow;
    }

    internal void Revoke()
    {
        Status = ApiKeyStatus.Revoked;
        RevokedOnUtc = DateTime.UtcNow;
    }

    internal void Expire()
    {
        Status = ApiKeyStatus.Expired;
    }
}
