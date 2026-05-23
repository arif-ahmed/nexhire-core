using System.Text.Json;
using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;

public enum RateWindow { PerMinute, PerHour, PerDay }
public enum PullInterval { Hourly, Daily, Weekly, Off }
public enum VerificationKind { Identity, Education, Employer }
public enum VerificationOutcome { Match, NoMatch }
public enum ApiVersionStatus { Active, Deprecated, Sunset }

public sealed record EmailAddress
{
    public string Value { get; private init; } = null!;
    private EmailAddress() { }
    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<EmailAddress>(new Error("Email.Invalid", "Email cannot be empty."));

        value = value.Trim().ToLowerInvariant();
        if (!value.Contains("@") || value.Length > 256)
            return Result.Failure<EmailAddress>(new Error("Email.Invalid", "Invalid email address format."));

        return Result.Success(new EmailAddress(value));
    }
}

public sealed record RateLimit
{
    public int MaxRequests { get; private init; }
    public RateWindow Window { get; private init; }

    private RateLimit() { }
    private RateLimit(int maxRequests, RateWindow window)
    {
        MaxRequests = maxRequests;
        Window = window;
    }

    public static Result<RateLimit> Create(int maxRequests, RateWindow window)
    {
        if (maxRequests <= 0)
            return Result.Failure<RateLimit>(new Error("RateLimit.Invalid", "Max requests must be greater than zero."));

        return Result.Success(new RateLimit(maxRequests, window));
    }
}

public sealed record EncryptedCredentials
{
    public string CipherText { get; private init; } = null!;
    public string KeyRef { get; private init; } = null!;
    public string Algorithm { get; private init; } = null!;

    private EncryptedCredentials() { }
    private EncryptedCredentials(string cipherText, string keyRef, string algorithm)
    {
        CipherText = cipherText;
        KeyRef = keyRef;
        Algorithm = algorithm;
    }

    public static Result<EncryptedCredentials> Create(string cipherText, string keyRef, string algorithm = "AES-256-GCM")
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return Result.Failure<EncryptedCredentials>(new Error("Credentials.Invalid", "Cipher text is required."));
        if (string.IsNullOrWhiteSpace(keyRef))
            return Result.Failure<EncryptedCredentials>(new Error("Credentials.Invalid", "Key reference is required."));
        if (algorithm != "AES-256-GCM")
            return Result.Failure<EncryptedCredentials>(new Error("Credentials.Invalid", "Only AES-256-GCM algorithm is supported."));

        return Result.Success(new EncryptedCredentials(cipherText, keyRef, algorithm));
    }

    public override string ToString() => "***";
}

public sealed record SyncOptions
{
    public PullInterval PullInterval { get; private init; }
    public bool PushOnPublish { get; private init; }
    public Guid? MappingProfileId { get; private init; }

    private SyncOptions() { }
    private SyncOptions(PullInterval pullInterval, bool pushOnPublish, Guid? mappingProfileId)
    {
        PullInterval = pullInterval;
        PushOnPublish = pushOnPublish;
        MappingProfileId = mappingProfileId;
    }

    public static SyncOptions Create(PullInterval pullInterval, bool pushOnPublish, Guid? mappingProfileId)
    {
        return new SyncOptions(pullInterval, pushOnPublish, mappingProfileId);
    }
}

public sealed record ExternalRef
{
    public string PortalName { get; private init; } = null!;
    public string ExternalJobId { get; private init; } = null!;

    private ExternalRef() { }
    private ExternalRef(string portalName, string externalJobId)
    {
        PortalName = portalName;
        ExternalJobId = externalJobId;
    }

    public static Result<ExternalRef> Create(string portalName, string externalJobId)
    {
        if (string.IsNullOrWhiteSpace(portalName))
            return Result.Failure<ExternalRef>(new Error("ExternalRef.Invalid", "Portal name is required."));
        if (string.IsNullOrWhiteSpace(externalJobId))
            return Result.Failure<ExternalRef>(new Error("ExternalRef.Invalid", "External job ID is required."));

        return Result.Success(new ExternalRef(portalName.Trim(), externalJobId.Trim()));
    }
}

public sealed record NormalisedLocation
{
    public string City { get; private init; } = null!;
    public string? District { get; private init; }
    public string Country { get; private init; } = null!;
    public double? Lat { get; private init; }
    public double? Lon { get; private init; }

    private NormalisedLocation() { }
    private NormalisedLocation(string city, string? district, string country, double? lat, double? lon)
    {
        City = city;
        District = district;
        Country = country;
        Lat = lat;
        Lon = lon;
    }

    public static Result<NormalisedLocation> Create(string city, string? district, string country, double? lat = null, double? lon = null)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure<NormalisedLocation>(new Error("Location.Invalid", "City is required."));
        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<NormalisedLocation>(new Error("Location.Invalid", "Country is required."));

        return Result.Success(new NormalisedLocation(city.Trim(), district?.Trim(), country.Trim(), lat, lon));
    }
}

public sealed record SalaryRange
{
    public decimal Min { get; private init; }
    public decimal Max { get; private init; }
    public string CurrencyCode { get; private init; } = null!;

    private SalaryRange() { }
    private SalaryRange(decimal min, decimal max, string currencyCode)
    {
        Min = min;
        Max = max;
        CurrencyCode = currencyCode;
    }

    public static Result<SalaryRange> Create(decimal min, decimal max, string currencyCode)
    {
        if (min < 0)
            return Result.Failure<SalaryRange>(new Error("Salary.Invalid", "Minimum salary cannot be negative."));
        if (min > max)
            return Result.Failure<SalaryRange>(new Error("Salary.Invalid", "Minimum salary cannot exceed maximum."));
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            return Result.Failure<SalaryRange>(new Error("Salary.Invalid", "Currency code must be a 3-character ISO 4217 code."));

        return Result.Success(new SalaryRange(min, max, currencyCode.Trim().ToUpperInvariant()));
    }
}

public sealed record SourceAttribution
{
    public string SourceName { get; private init; } = null!;
    public string? Backlink { get; private init; }
    public bool IsPublic { get; private init; }

    private SourceAttribution() { }
    private SourceAttribution(string sourceName, string? backlink, bool isPublic)
    {
        SourceName = sourceName;
        Backlink = backlink;
        IsPublic = isPublic;
    }

    public static Result<SourceAttribution> Create(string sourceName, string? backlink = null, bool isPublic = true)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return Result.Failure<SourceAttribution>(new Error("SourceAttribution.Invalid", "Source name is required."));

        return Result.Success(new SourceAttribution(sourceName.Trim(), backlink?.Trim(), isPublic));
    }
}

public sealed record NormalisedJobPosting
{
    public string Title { get; private init; } = null!;
    public string Description { get; private init; } = null!;
    public NormalisedLocation Location { get; private init; } = null!;
    public SalaryRange? SalaryRange { get; private init; }
    public string EmploymentType { get; private init; } = null!;
    public List<string> Requirements { get; private init; } = new();
    public List<string> SkillCodes { get; private init; } = new();
    public SourceAttribution SourceAttribution { get; private init; } = null!;
    public ExternalRef ExternalRef { get; private init; } = null!;
    public DateTime PostedOnUtc { get; private init; }
    public DateTime? DeadlineUtc { get; private init; }

    private NormalisedJobPosting() { }
    private NormalisedJobPosting(
        string title, 
        string description, 
        NormalisedLocation location, 
        SalaryRange? salaryRange, 
        string employmentType, 
        List<string> requirements, 
        List<string> skillCodes, 
        SourceAttribution sourceAttribution, 
        ExternalRef externalRef, 
        DateTime postedOnUtc, 
        DateTime? deadlineUtc)
    {
        Title = title;
        Description = description;
        Location = location;
        SalaryRange = salaryRange;
        EmploymentType = employmentType;
        Requirements = requirements;
        SkillCodes = skillCodes;
        SourceAttribution = sourceAttribution;
        ExternalRef = externalRef;
        PostedOnUtc = postedOnUtc;
        DeadlineUtc = deadlineUtc;
    }

    public static Result<NormalisedJobPosting> Create(
        string title, 
        string description, 
        NormalisedLocation location, 
        SalaryRange? salaryRange, 
        string employmentType, 
        List<string> requirements, 
        List<string> skillCodes, 
        SourceAttribution sourceAttribution, 
        ExternalRef externalRef, 
        DateTime postedOnUtc, 
        DateTime? deadlineUtc = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<NormalisedJobPosting>(new Error("JobPosting.Invalid", "Title is required."));
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<NormalisedJobPosting>(new Error("JobPosting.Invalid", "Description is required."));
        if (location == null)
            return Result.Failure<NormalisedJobPosting>(new Error("JobPosting.Invalid", "Location is required."));
        if (sourceAttribution == null)
            return Result.Failure<NormalisedJobPosting>(new Error("JobPosting.Invalid", "Source attribution is required."));
        if (externalRef == null)
            return Result.Failure<NormalisedJobPosting>(new Error("JobPosting.Invalid", "External reference is required."));

        return Result.Success(new NormalisedJobPosting(
            title.Trim(), 
            description.Trim(), 
            location, 
            salaryRange, 
            string.IsNullOrWhiteSpace(employmentType) ? "FullTime" : employmentType.Trim(), 
            requirements ?? new(), 
            skillCodes ?? new(), 
            sourceAttribution, 
            externalRef, 
            postedOnUtc, 
            deadlineUtc));
    }
}

public sealed record Registry
{
    public string Name { get; private init; } = null!;
    public string Endpoint { get; private init; } = null!;

    private Registry() { }
    private Registry(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    public static Result<Registry> Create(string name, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Registry>(new Error("Registry.Invalid", "Registry name is required."));
        if (string.IsNullOrWhiteSpace(endpoint))
            return Result.Failure<Registry>(new Error("Registry.Invalid", "Registry endpoint is required."));

        return Result.Success(new Registry(name.Trim(), endpoint.Trim()));
    }
}

public sealed record ConsentRecord
{
    public bool Granted { get; private init; }
    public string ConsentVersion { get; private init; } = null!;
    public DateTime RecordedOnUtc { get; private init; }

    private ConsentRecord() { }
    private ConsentRecord(bool granted, string consentVersion, DateTime recordedOnUtc)
    {
        Granted = granted;
        ConsentVersion = consentVersion;
        RecordedOnUtc = recordedOnUtc;
    }

    public static Result<ConsentRecord> Create(bool granted, string consentVersion, DateTime recordedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(consentVersion))
            return Result.Failure<ConsentRecord>(new Error("Consent.Invalid", "Consent version is required."));

        return Result.Success(new ConsentRecord(granted, consentVersion.Trim(), recordedOnUtc));
    }
}

public sealed record MinimisedRequestPayload
{
    public Dictionary<string, string> Fields { get; private init; } = new();

    private MinimisedRequestPayload() { }
    private MinimisedRequestPayload(Dictionary<string, string> fields) => Fields = fields;

    private static readonly Dictionary<VerificationKind, HashSet<string>> Whitelists = new()
    {
        { VerificationKind.Identity, new() { "id_number", "id_type" } },
        { VerificationKind.Education, new() { "student_id", "institution_name", "degree_name" } },
        { VerificationKind.Employer, new() { "registration_number", "tax_id" } }
    };

    public static Result<MinimisedRequestPayload> Create(VerificationKind kind, Dictionary<string, string> fields)
    {
        if (fields == null)
            return Result.Failure<MinimisedRequestPayload>(new Error("Payload.Invalid", "Payload fields cannot be null."));

        var whitelist = Whitelists[kind];
        foreach (var key in fields.Keys)
        {
            if (!whitelist.Contains(key))
            {
                return Result.Failure<MinimisedRequestPayload>(
                    new Error("Payload.NotWhitelisted", $"Field '{key}' is not whitelisted for verification kind '{kind}'."));
            }
        }

        return Result.Success(new MinimisedRequestPayload(new Dictionary<string, string>(fields)));
    }
}

public sealed record VerificationResult
{
    public VerificationOutcome Outcome { get; private init; }
    public string? CredentialRef { get; private init; }
    public string RegistrySource { get; private init; } = null!;
    public DateTime RespondedOnUtc { get; private init; }

    private VerificationResult() { }
    private VerificationResult(VerificationOutcome outcome, string? credentialRef, string registrySource, DateTime respondedOnUtc)
    {
        Outcome = outcome;
        CredentialRef = credentialRef;
        RegistrySource = registrySource;
        RespondedOnUtc = respondedOnUtc;
    }

    public static Result<VerificationResult> Create(VerificationOutcome outcome, string? credentialRef, string registrySource, DateTime respondedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(registrySource))
            return Result.Failure<VerificationResult>(new Error("VerificationResult.Invalid", "Registry source is required."));

        return Result.Success(new VerificationResult(outcome, credentialRef?.Trim(), registrySource.Trim(), respondedOnUtc));
    }
}

public sealed record WebhookSignature
{
    public string Algorithm { get; private init; } = null!;
    public string SignatureValue { get; private init; } = null!;

    private WebhookSignature() { }
    private WebhookSignature(string algorithm, string signatureValue)
    {
        Algorithm = algorithm;
        SignatureValue = signatureValue;
    }

    public static Result<WebhookSignature> Create(string algorithm, string signatureValue)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            return Result.Failure<WebhookSignature>(new Error("WebhookSignature.Invalid", "Algorithm is required."));
        if (string.IsNullOrWhiteSpace(signatureValue))
            return Result.Failure<WebhookSignature>(new Error("WebhookSignature.Invalid", "Signature value is required."));

        return Result.Success(new WebhookSignature(algorithm.Trim(), signatureValue.Trim()));
    }
}

public sealed record ApiVersion
{
    public Guid Id { get; private init; }
    public string Version { get; private init; } = null!;
    public ApiVersionStatus Status { get; private init; }
    public DateTime ReleasedOnUtc { get; private init; }
    public DateTime? DeprecationAnnouncedOnUtc { get; private init; }
    public DateTime? SunsetOnUtc { get; private init; }
    public string? MigrationGuideUrl { get; private init; }

    private ApiVersion() { }
    private ApiVersion(
        Guid id, 
        string version, 
        ApiVersionStatus status, 
        DateTime releasedOnUtc, 
        DateTime? deprecationAnnouncedOnUtc, 
        DateTime? sunsetOnUtc, 
        string? migrationGuideUrl)
    {
        Id = id;
        Version = version;
        Status = status;
        ReleasedOnUtc = releasedOnUtc;
        DeprecationAnnouncedOnUtc = deprecationAnnouncedOnUtc;
        SunsetOnUtc = sunsetOnUtc;
        MigrationGuideUrl = migrationGuideUrl;
    }

    public static Result<ApiVersion> Create(
        string version, 
        ApiVersionStatus status, 
        DateTime releasedOnUtc, 
        DateTime? deprecationAnnouncedOnUtc = null, 
        DateTime? sunsetOnUtc = null, 
        string? migrationGuideUrl = null)
    {
        if (string.IsNullOrWhiteSpace(version) || !Regex.IsMatch(version, @"^v\d+$"))
            return Result.Failure<ApiVersion>(new Error("ApiVersion.Invalid", "Version must match pattern '^v\\d+$' (e.g., v1)."));

        return Result.Success(new ApiVersion(
            Guid.NewGuid(), 
            version, 
            status, 
            releasedOnUtc, 
            deprecationAnnouncedOnUtc, 
            sunsetOnUtc, 
            migrationGuideUrl?.Trim()));
    }

    public ApiVersion Deprecate(DateTime sunsetOnUtc, string? migrationGuideUrl) =>
        new(Id, Version, ApiVersionStatus.Deprecated, ReleasedOnUtc, DateTime.UtcNow, sunsetOnUtc, migrationGuideUrl);

    public ApiVersion Sunset() =>
        new(Id, Version, ApiVersionStatus.Sunset, ReleasedOnUtc, DeprecationAnnouncedOnUtc, SunsetOnUtc, MigrationGuideUrl);
}
