using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

public enum PostingStatus { Draft, Active, Paused, Expired, Suspended, Removed, Archived }
public enum ContractType { FullTime, PartTime, Training, ProjectBased }
public enum EducationLevel { None, Secondary, Diploma, Bachelor, Master, Doctorate }
public enum WorkFormat { Physical, Online, Hybrid }
public enum SkillImportance { Mandatory, Preferred }
public enum SalaryPeriod { Monthly, Yearly }
public enum VisibilityLevel { Public, Private, Targeted }
public enum AuditActorKind { Employer, Admin, System }
public enum AuditEntryKind { StatusChange, FieldEdit }

public sealed record JobTitle
{
    public string Value { get; private init; }
    private JobTitle(string value) => Value = value;
    public static Result<JobTitle> Create(string value)
    {
        value = (value ?? string.Empty).Trim();
        return value.Length is < 3 or > 150
            ? Result.Failure<JobTitle>(new Error("E-POST-TITLE-INVALID", "Job title must be between 3 and 150 characters."))
            : Result.Success(new JobTitle(value));
    }
}

public sealed record JobSummary
{
    public string Value { get; private init; }
    private JobSummary(string value) => Value = value;
    public static Result<JobSummary> Create(string value)
    {
        value = (value ?? string.Empty).Trim();
        return value.Length is < 20 or > 5000
            ? Result.Failure<JobSummary>(new Error("E-POST-SUMMARY-INVALID", "Job summary must be between 20 and 5000 characters."))
            : Result.Success(new JobSummary(value));
    }
}

public sealed record EmploymentLocation
{
    public string Line1 { get; private init; }
    public string City { get; private init; }
    public string District { get; private init; }
    public string Country { get; private init; }

    private EmploymentLocation(string line1, string city, string district, string country)
    {
        Line1 = line1;
        City = city;
        District = district;
        Country = country;
    }

    public static Result<EmploymentLocation> Create(string line1, string city, string district, string country)
    {
        if (string.IsNullOrWhiteSpace(line1) || string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<EmploymentLocation>(new Error("E-POST-LOCATION-INVALID", "Location fields are required."));
        }

        return Result.Success(new EmploymentLocation(line1.Trim(), city.Trim(), district.Trim(), country.Trim()));
    }
}

public sealed record CanonicalSkillRef
{
    public string TaxonomyCode { get; private init; }
    public string DisplayLabel { get; private init; }
    private CanonicalSkillRef(string taxonomyCode, string displayLabel)
    {
        TaxonomyCode = taxonomyCode;
        DisplayLabel = displayLabel;
    }

    public static Result<CanonicalSkillRef> Create(string taxonomyCode, string displayLabel)
    {
        if (string.IsNullOrWhiteSpace(taxonomyCode) || string.IsNullOrWhiteSpace(displayLabel))
        {
            return Result.Failure<CanonicalSkillRef>(new Error("E-POST-SKILL-INVALID", "Skill code and label are required."));
        }

        return Result.Success(new CanonicalSkillRef(taxonomyCode.Trim(), displayLabel.Trim()));
    }
}

public sealed record RequiredSkill
{
    public CanonicalSkillRef CanonicalRef { get; private init; }
    public string RawLabel { get; private init; }
    public SkillImportance Importance { get; private init; }

    private RequiredSkill(CanonicalSkillRef canonicalRef, string rawLabel, SkillImportance importance)
    {
        CanonicalRef = canonicalRef;
        RawLabel = rawLabel;
        Importance = importance;
    }

    public static Result<RequiredSkill> Create(CanonicalSkillRef canonicalRef, string rawLabel, SkillImportance importance)
    {
        return string.IsNullOrWhiteSpace(rawLabel)
            ? Result.Failure<RequiredSkill>(new Error("E-POST-SKILL-INVALID", "Raw skill label is required."))
            : Result.Success(new RequiredSkill(canonicalRef, rawLabel.Trim(), importance));
    }
}

public sealed record LanguageRequirement
{
    public string Language { get; private init; }
    public string Proficiency { get; private init; }
    private LanguageRequirement(string language, string proficiency)
    {
        Language = language;
        Proficiency = proficiency;
    }

    public static Result<LanguageRequirement> Create(string language, string proficiency)
    {
        if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(proficiency))
        {
            return Result.Failure<LanguageRequirement>(new Error("E-POST-LANGUAGE-INVALID", "Language and proficiency are required."));
        }

        return Result.Success(new LanguageRequirement(language.Trim(), proficiency.Trim()));
    }
}

public sealed record SalaryRange
{
    public decimal Min { get; private init; }
    public decimal Max { get; private init; }
    public string Currency { get; private init; }
    public SalaryPeriod Period { get; private init; }

    private SalaryRange(decimal min, decimal max, string currency, SalaryPeriod period)
    {
        Min = min;
        Max = max;
        Currency = currency;
        Period = period;
    }

    public static Result<SalaryRange> Create(decimal min, decimal max, string? currency = "BDT", SalaryPeriod period = SalaryPeriod.Monthly)
    {
        if (min < 0 || max < 0 || min > max)
        {
            return Result.Failure<SalaryRange>(new Error("E-POST-SALARY-INVALID", "Salary range must be non-negative and min must not exceed max."));
        }

        return Result.Success(new SalaryRange(min, max, string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim(), period));
    }
}

public sealed record ApplicationDeadline
{
    public DateTime DateUtc { get; private init; }
    public bool AutoCloseEnabled { get; private init; }

    private ApplicationDeadline(DateTime dateUtc, bool autoCloseEnabled)
    {
        DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
        AutoCloseEnabled = autoCloseEnabled;
    }

    public static Result<ApplicationDeadline> Create(DateTime dateUtc, bool autoCloseEnabled, DateTime? nowUtc = null)
    {
        var normalized = dateUtc.Kind == DateTimeKind.Utc ? dateUtc : DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
        if (normalized <= (nowUtc ?? DateTime.UtcNow))
        {
            return Result.Failure<ApplicationDeadline>(new Error("E-POST-DEADLINE-IN-PAST", "Application deadline must be in the future."));
        }

        return Result.Success(new ApplicationDeadline(normalized, autoCloseEnabled));
    }

    public static ApplicationDeadline Rehydrate(DateTime dateUtc, bool autoCloseEnabled) => new(dateUtc, autoCloseEnabled);
}

public sealed record JobPostingLink
{
    public string Url { get; private init; }
    private JobPostingLink(string url) => Url = url;
    public static Result<JobPostingLink> Create(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? Result.Success(new JobPostingLink(uri.ToString()))
            : Result.Failure<JobPostingLink>(new Error("E-POST-LINK-INVALID", "Job link must be an absolute HTTPS URL."));
    }
}

public sealed record TargetingCriteria
{
    public IReadOnlyCollection<string> SkillCodes { get; private init; }
    public IReadOnlyCollection<string> Locations { get; private init; }
    public IReadOnlyCollection<Guid> SeekerGroupIds { get; private init; }

    private TargetingCriteria(IReadOnlyCollection<string> skillCodes, IReadOnlyCollection<string> locations, IReadOnlyCollection<Guid> seekerGroupIds)
    {
        SkillCodes = skillCodes;
        Locations = locations;
        SeekerGroupIds = seekerGroupIds;
    }

    public bool IsEmpty => !SkillCodes.Any() && !Locations.Any() && !SeekerGroupIds.Any();

    public static Result<TargetingCriteria> Create(IEnumerable<string>? skillCodes, IEnumerable<string>? locations, IEnumerable<Guid>? seekerGroupIds)
    {
        var criteria = new TargetingCriteria(
            (skillCodes ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray(),
            (locations ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray(),
            (seekerGroupIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray());

        return criteria.IsEmpty
            ? Result.Failure<TargetingCriteria>(new Error("E-POST-TARGETING-REQUIRED", "Targeted visibility requires targeting criteria."))
            : Result.Success(criteria);
    }
}

public sealed record PostingVisibility
{
    public VisibilityLevel Level { get; private init; }
    public TargetingCriteria? TargetingCriteria { get; private init; }

    private PostingVisibility(VisibilityLevel level, TargetingCriteria? targetingCriteria)
    {
        Level = level;
        TargetingCriteria = targetingCriteria;
    }

    public static Result<PostingVisibility> Create(VisibilityLevel level, TargetingCriteria? targetingCriteria = null)
    {
        if (level == VisibilityLevel.Targeted && (targetingCriteria is null || targetingCriteria.IsEmpty))
        {
            return Result.Failure<PostingVisibility>(new Error("E-POST-TARGETING-REQUIRED", "Targeted visibility requires targeting criteria."));
        }

        if (level != VisibilityLevel.Targeted && targetingCriteria is not null)
        {
            return Result.Failure<PostingVisibility>(new Error("E-POST-TARGETING-NOT-ALLOWED", "Only targeted visibility may include targeting criteria."));
        }

        return Result.Success(new PostingVisibility(level, targetingCriteria));
    }
}

public sealed record SchemaOrgJobPosting
{
    public IReadOnlyDictionary<string, string> Properties { get; private init; }
    public bool IsCompliant { get; private init; }
    public IReadOnlyCollection<string> Violations { get; private init; }

    private SchemaOrgJobPosting(IReadOnlyDictionary<string, string> properties, bool isCompliant, IReadOnlyCollection<string> violations)
    {
        Properties = properties;
        IsCompliant = isCompliant;
        Violations = violations;
    }

    public static SchemaOrgJobPosting Create(IReadOnlyDictionary<string, string> properties, IReadOnlyCollection<string> violations) =>
        new(properties, violations.Count == 0, violations);
}

public sealed record StatusTransition
{
    public PostingStatus From { get; private init; }
    public PostingStatus To { get; private init; }

    private StatusTransition(PostingStatus from, PostingStatus to)
    {
        From = from;
        To = to;
    }

    public static Result<StatusTransition> Create(PostingStatus from, PostingStatus to)
    {
        return JobPostingStatusRules.CanTransition(from, to)
            ? Result.Success(new StatusTransition(from, to))
            : Result.Failure<StatusTransition>(new Error("E-POST-ILLEGAL-TRANSITION", $"Cannot transition from {from} to {to}."));
    }
}

public sealed record AuditActor
{
    public AuditActorKind Kind { get; private init; }
    public Guid? UserId { get; private init; }
    public string DisplayName { get; private init; }

    private AuditActor(AuditActorKind kind, Guid? userId, string displayName)
    {
        Kind = kind;
        UserId = userId;
        DisplayName = displayName;
    }

    public static Result<AuditActor> Create(AuditActorKind kind, Guid? userId, string displayName)
    {
        if (kind != AuditActorKind.System && (userId is null || userId == Guid.Empty))
        {
            return Result.Failure<AuditActor>(new Error("E-POST-ACTOR-INVALID", "User id is required for employer and admin actors."));
        }

        return Result.Success(new AuditActor(kind, userId, string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName.Trim()));
    }

    public static AuditActor System() => new(AuditActorKind.System, null, "System");
}

public static class JobPostingStatusRules
{
    public static bool IsTerminal(PostingStatus status) => status is PostingStatus.Archived or PostingStatus.Removed;
    public static bool IsEditable(PostingStatus status) => status is PostingStatus.Draft or PostingStatus.Active or PostingStatus.Paused;
    public static bool IsSearchable(PostingStatus status) => status == PostingStatus.Active;
    public static bool IsAcceptingApplications(PostingStatus status) => status == PostingStatus.Active;

    public static bool CanTransition(PostingStatus from, PostingStatus to) => (from, to) switch
    {
        (PostingStatus.Draft, PostingStatus.Active) => true,
        (PostingStatus.Draft, PostingStatus.Removed) => true,
        (PostingStatus.Active, PostingStatus.Paused) => true,
        (PostingStatus.Paused, PostingStatus.Active) => true,
        (PostingStatus.Active, PostingStatus.Expired) => true,
        (PostingStatus.Paused, PostingStatus.Expired) => true,
        (PostingStatus.Active, PostingStatus.Suspended) => true,
        (PostingStatus.Paused, PostingStatus.Suspended) => true,
        (PostingStatus.Suspended, PostingStatus.Active) => true,
        (PostingStatus.Expired, PostingStatus.Archived) => true,
        (PostingStatus.Active, PostingStatus.Archived) => true,
        (PostingStatus.Paused, PostingStatus.Archived) => true,
        (PostingStatus.Active or PostingStatus.Paused or PostingStatus.Expired or PostingStatus.Suspended, PostingStatus.Removed) => true,
        _ => false
    };
}
