using Nexhire.Modules.JobPostings.Core.Domain.Events;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.Domain.Aggregates;

public sealed class JobPosting : AggregateRoot<Guid>
{
    private readonly List<RequiredSkill> _requiredSkills = new();
    private readonly List<LanguageRequirement> _requiredLanguages = new();
    private readonly List<string> _deprecatedSkillCodes = new();

    public Guid EmployerId { get; private set; }
    public Guid PostedByUserId { get; private set; }
    public PostingStatus Status { get; private set; }
    public JobTitle Title { get; private set; } = null!;
    public JobSummary Summary { get; private set; } = null!;
    public ContractType ContractType { get; private set; }
    public EducationLevel EducationLevel { get; private set; }
    public WorkFormat WorkFormat { get; private set; }
    public EmploymentLocation? Location { get; private set; }
    public IReadOnlyCollection<RequiredSkill> RequiredSkills => _requiredSkills.AsReadOnly();
    public IReadOnlyCollection<LanguageRequirement> RequiredLanguages => _requiredLanguages.AsReadOnly();
    public IReadOnlyCollection<string> DeprecatedSkillCodes => _deprecatedSkillCodes.AsReadOnly();
    public SalaryRange? SalaryRange { get; private set; }
    public ApplicationDeadline Deadline { get; private set; } = null!;
    public JobPostingLink? JobLink { get; private set; }
    public PostingVisibility Visibility { get; private set; } = null!;
    public SchemaOrgJobPosting? SchemaOrg { get; private set; }
    public Guid? RenewedFromPostingId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public DateTime? PublishedOnUtc { get; private set; }
    public string? ExternalRef { get; private set; }

    private JobPosting() { }

    private JobPosting(Guid id) : base(id) { }

    public static Result<JobPosting> CreateDraft(
        Guid employerId,
        Guid postedByUserId,
        JobTitle title,
        JobSummary summary,
        ContractType contractType,
        EducationLevel educationLevel,
        WorkFormat workFormat,
        EmploymentLocation? location,
        IEnumerable<RequiredSkill> requiredSkills,
        IEnumerable<LanguageRequirement> requiredLanguages,
        ApplicationDeadline deadline,
        JobPostingLink? jobLink,
        SalaryRange? salaryRange,
        PostingVisibility visibility,
        DateTime? nowUtc = null,
        Guid? id = null)
    {
        if (employerId == Guid.Empty || postedByUserId == Guid.Empty)
        {
            return Result.Failure<JobPosting>(new Error("E-POST-ACTOR-INVALID", "Employer and user ids are required."));
        }

        if (workFormat == WorkFormat.Physical && location is null)
        {
            return Result.Failure<JobPosting>(new Error("E-POST-LOCATION-REQUIRED", "Physical postings require a location."));
        }

        var posting = new JobPosting(id ?? Guid.NewGuid())
        {
            EmployerId = employerId,
            PostedByUserId = postedByUserId,
            Status = PostingStatus.Draft,
            CreatedOnUtc = nowUtc ?? DateTime.UtcNow
        };

        posting.ApplyDetails(title, summary, contractType, educationLevel, workFormat, location, requiredSkills, requiredLanguages, deadline, jobLink, salaryRange, visibility);
        posting.UpdatedOnUtc = posting.CreatedOnUtc;
        posting.RaiseDomainEvent(new JobPostingCreatedIntegrationEvent(posting.Id, posting.EmployerId, posting.CreatedOnUtc));
        return Result.Success(posting);
    }

    public Result Publish(SchemaOrgJobPosting schemaOrg, EmployerStanding employerStanding, DateTime? nowUtc = null)
    {
        var transition = EnsureTransition(PostingStatus.Active);
        if (transition.IsFailure) return transition;
        if (!employerStanding.MayPublish)
        {
            return Result.Failure(new Error("E-POST-EMPLOYER-NOT-ELIGIBLE", "Employer is not eligible to publish postings."));
        }
        if (!schemaOrg.IsCompliant)
        {
            return Result.Failure(new Error("E-POST-NOT-SCHEMA-COMPLIANT", "Posting is not Schema.org compliant."));
        }
        if (Deadline.DateUtc <= (nowUtc ?? DateTime.UtcNow))
        {
            return Result.Failure(new Error("E-POST-DEADLINE-IN-PAST", "Application deadline is in the past."));
        }

        var occurred = nowUtc ?? DateTime.UtcNow;
        Status = PostingStatus.Active;
        SchemaOrg = schemaOrg;
        PublishedOnUtc = occurred;
        Touch(occurred);
        RaiseDomainEvent(new JobPostingPublishedIntegrationEvent(Id, EmployerId, Title.Value, ContractType.ToString(), WorkFormat.ToString(), RequiredSkills.Select(x => x.CanonicalRef.TaxonomyCode).ToArray(), Deadline.DateUtc, Visibility.Level.ToString(), occurred));
        RaiseStatusChanged(PostingStatus.Draft, Status, AuditActorKind.Employer, occurred);
        return Result.Success();
    }

    public Result<IReadOnlyCollection<string>> EditDetails(
        JobTitle title,
        JobSummary summary,
        ContractType contractType,
        EducationLevel educationLevel,
        WorkFormat workFormat,
        EmploymentLocation? location,
        IEnumerable<RequiredSkill> requiredSkills,
        IEnumerable<LanguageRequirement> requiredLanguages,
        ApplicationDeadline deadline,
        JobPostingLink? jobLink,
        SalaryRange? salaryRange,
        PostingVisibility visibility,
        SchemaOrgJobPosting? schemaOrg,
        DateTime? nowUtc = null)
    {
        if (!JobPostingStatusRules.IsEditable(Status))
        {
            return Result.Failure<IReadOnlyCollection<string>>(new Error("E-POST-ILLEGAL-TRANSITION", "Posting cannot be edited in its current status."));
        }
        if (workFormat == WorkFormat.Physical && location is null)
        {
            return Result.Failure<IReadOnlyCollection<string>>(new Error("E-POST-LOCATION-REQUIRED", "Physical postings require a location."));
        }

        var changed = DetectChangedFields(title, summary, contractType, educationLevel, workFormat, location, requiredSkills, requiredLanguages, deadline, jobLink, salaryRange, visibility);
        ApplyDetails(title, summary, contractType, educationLevel, workFormat, location, requiredSkills, requiredLanguages, deadline, jobLink, salaryRange, visibility);
        if (Status == PostingStatus.Active)
        {
            if (schemaOrg is null || !schemaOrg.IsCompliant)
            {
                return Result.Failure<IReadOnlyCollection<string>>(new Error("E-POST-NOT-SCHEMA-COMPLIANT", "Active posting edits must remain Schema.org compliant."));
            }
            SchemaOrg = schemaOrg;
            RaiseDomainEvent(new JobPostingUpdatedIntegrationEvent(Id, changed.ToArray(), RequiredSkills.Select(x => x.CanonicalRef.TaxonomyCode).ToArray(), nowUtc ?? DateTime.UtcNow));
        }

        Touch(nowUtc);
        return Result.Success<IReadOnlyCollection<string>>(changed);
    }

    public Result ExtendDeadline(ApplicationDeadline newDeadline, DateTime? nowUtc = null)
    {
        if (!JobPostingStatusRules.IsEditable(Status))
        {
            return Result.Failure(new Error("E-POST-ILLEGAL-TRANSITION", "Deadline cannot be changed in the current status."));
        }
        if (newDeadline.DateUtc <= Deadline.DateUtc)
        {
            return Result.Failure(new Error("E-POST-DEADLINE-NOT-LATER", "New deadline must be later than the current deadline."));
        }

        Deadline = newDeadline;
        Touch(nowUtc);
        return Result.Success();
    }

    public Result SetVisibility(PostingVisibility visibility, DateTime? nowUtc = null)
    {
        if (!JobPostingStatusRules.IsEditable(Status))
        {
            return Result.Failure(new Error("E-POST-ILLEGAL-TRANSITION", "Visibility cannot be changed in the current status."));
        }

        Visibility = visibility;
        Touch(nowUtc);
        return Result.Success();
    }

    public Result<IReadOnlyCollection<string>> UpdateRequiredSkills(IEnumerable<RequiredSkill> requiredSkills, SchemaOrgJobPosting? schemaOrg, DateTime? nowUtc = null)
    {
        if (!JobPostingStatusRules.IsEditable(Status))
        {
            return Result.Failure<IReadOnlyCollection<string>>(new Error("E-POST-ILLEGAL-TRANSITION", "Skills cannot be changed in the current status."));
        }

        var incoming = requiredSkills.ToArray();
        if (_requiredSkills.SequenceEqual(incoming))
        {
            return Result.Success<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        _requiredSkills.Clear();
        _requiredSkills.AddRange(incoming);

        if (Status == PostingStatus.Active)
        {
            if (schemaOrg is null || !schemaOrg.IsCompliant)
            {
                return Result.Failure<IReadOnlyCollection<string>>(new Error("E-POST-NOT-SCHEMA-COMPLIANT", "Active posting edits must remain Schema.org compliant."));
            }
            SchemaOrg = schemaOrg;
            RaiseDomainEvent(new JobPostingUpdatedIntegrationEvent(Id, new[] { nameof(RequiredSkills) }, RequiredSkills.Select(x => x.CanonicalRef.TaxonomyCode).ToArray(), nowUtc ?? DateTime.UtcNow));
        }

        Touch(nowUtc);
        return Result.Success<IReadOnlyCollection<string>>(new[] { nameof(RequiredSkills) });
    }

    public Result Pause(DateTime? nowUtc = null) => ChangeStatus(PostingStatus.Paused, AuditActorKind.Employer, nowUtc);
    public Result Resume(DateTime? nowUtc = null) => ChangeStatus(PostingStatus.Active, AuditActorKind.Employer, nowUtc);
    public Result Expire(DateTime? nowUtc = null) => ChangeStatus(PostingStatus.Expired, AuditActorKind.System, nowUtc);
    public Result Archive(DateTime? nowUtc = null)
    {
        if (Status != PostingStatus.Expired)
        {
            return Result.Failure(new Error("E-POST-ILLEGAL-TRANSITION", "Only expired postings can be archived by an employer."));
        }

        return ChangeStatus(PostingStatus.Archived, AuditActorKind.Employer, nowUtc);
    }
    public Result Reinstate(DateTime? nowUtc = null) => ChangeStatus(PostingStatus.Active, AuditActorKind.Admin, nowUtc);

    public Result Suspend(string reason, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("E-POST-REASON-REQUIRED", "A reason is required."));
        }
        return ChangeStatus(PostingStatus.Suspended, AuditActorKind.Admin, nowUtc, reason);
    }

    public Result Remove(string reason, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("E-POST-REASON-REQUIRED", "A reason is required."));
        }
        return ChangeStatus(PostingStatus.Removed, AuditActorKind.Admin, nowUtc, reason);
    }

    public Result CloseDueToEmployerStanding(string reason, DateTime? nowUtc = null)
    {
        if (Status is not (PostingStatus.Active or PostingStatus.Paused))
        {
            return Result.Success();
        }

        var from = Status;
        var occurred = nowUtc ?? DateTime.UtcNow;
        Status = PostingStatus.Archived;
        Touch(occurred);
        RaiseDomainEvent(new JobPostingClosedIntegrationEvent(Id, EmployerId, reason, occurred));
        RaiseStatusChanged(from, Status, AuditActorKind.System, occurred);
        return Result.Success();
    }

    public IReadOnlyCollection<string> FlagDeprecatedSkillCodes(IEnumerable<string> deprecatedSkillCodes, DateTime? nowUtc = null)
    {
        var matched = RequiredSkills
            .Select(x => x.CanonicalRef.TaxonomyCode)
            .Intersect(deprecatedSkillCodes.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var code in matched.Where(code => !_deprecatedSkillCodes.Contains(code, StringComparer.OrdinalIgnoreCase)))
        {
            _deprecatedSkillCodes.Add(code);
        }

        if (matched.Length > 0)
        {
            Touch(nowUtc);
        }

        return matched;
    }

    public static Result<JobPosting> RenewFrom(JobPosting expiredPosting, ApplicationDeadline newDeadline, DateTime? nowUtc = null)
    {
        if (expiredPosting.Status != PostingStatus.Expired)
        {
            return Result.Failure<JobPosting>(new Error("E-POST-ILLEGAL-TRANSITION", "Only expired postings can be renewed."));
        }

        var renewed = CreateDraft(
            expiredPosting.EmployerId,
            expiredPosting.PostedByUserId,
            expiredPosting.Title,
            expiredPosting.Summary,
            expiredPosting.ContractType,
            expiredPosting.EducationLevel,
            expiredPosting.WorkFormat,
            expiredPosting.Location,
            expiredPosting.RequiredSkills,
            expiredPosting.RequiredLanguages,
            newDeadline,
            expiredPosting.JobLink,
            expiredPosting.SalaryRange,
            expiredPosting.Visibility,
            nowUtc);

        if (renewed.IsFailure) return renewed;
        renewed.Value.RenewedFromPostingId = expiredPosting.Id;
        renewed.Value.RaiseDomainEvent(new JobPostingRenewedEvent(renewed.Value.Id, expiredPosting.Id, nowUtc ?? DateTime.UtcNow));
        return renewed;
    }

    public void MarkExternalMirror(string externalRef)
    {
        ExternalRef = externalRef;
    }

    private Result ChangeStatus(PostingStatus target, AuditActorKind actorKind, DateTime? nowUtc, string? reason = null)
    {
        var from = Status;
        var transition = EnsureTransition(target);
        if (transition.IsFailure) return transition;

        var occurred = nowUtc ?? DateTime.UtcNow;
        Status = target;
        Touch(occurred);

        if (target == PostingStatus.Expired)
        {
            RaiseDomainEvent(new JobPostingExpiredIntegrationEvent(Id, EmployerId, occurred));
        }
        else if (target == PostingStatus.Archived)
        {
            RaiseDomainEvent(new JobPostingClosedIntegrationEvent(Id, EmployerId, "archived", occurred));
        }
        else if (target == PostingStatus.Suspended)
        {
            RaiseDomainEvent(new JobPostingSuspendedIntegrationEvent(Id, EmployerId, reason!, occurred));
        }
        else if (from == PostingStatus.Suspended && target == PostingStatus.Active)
        {
            RaiseDomainEvent(new JobPostingReinstatedIntegrationEvent(Id, EmployerId, occurred));
        }

        RaiseStatusChanged(from, target, actorKind, occurred);
        return Result.Success();
    }

    private Result EnsureTransition(PostingStatus target)
    {
        return JobPostingStatusRules.CanTransition(Status, target)
            ? Result.Success()
            : Result.Failure(new Error("E-POST-ILLEGAL-TRANSITION", $"Cannot transition from {Status} to {target}."));
    }

    private void ApplyDetails(
        JobTitle title,
        JobSummary summary,
        ContractType contractType,
        EducationLevel educationLevel,
        WorkFormat workFormat,
        EmploymentLocation? location,
        IEnumerable<RequiredSkill> requiredSkills,
        IEnumerable<LanguageRequirement> requiredLanguages,
        ApplicationDeadline deadline,
        JobPostingLink? jobLink,
        SalaryRange? salaryRange,
        PostingVisibility visibility)
    {
        Title = title;
        Summary = summary;
        ContractType = contractType;
        EducationLevel = educationLevel;
        WorkFormat = workFormat;
        Location = location;
        Deadline = deadline;
        JobLink = jobLink;
        SalaryRange = salaryRange;
        Visibility = visibility;
        _requiredSkills.Clear();
        _requiredSkills.AddRange(requiredSkills);
        _requiredLanguages.Clear();
        _requiredLanguages.AddRange(requiredLanguages);
    }

    private List<string> DetectChangedFields(
        JobTitle title,
        JobSummary summary,
        ContractType contractType,
        EducationLevel educationLevel,
        WorkFormat workFormat,
        EmploymentLocation? location,
        IEnumerable<RequiredSkill> requiredSkills,
        IEnumerable<LanguageRequirement> requiredLanguages,
        ApplicationDeadline deadline,
        JobPostingLink? jobLink,
        SalaryRange? salaryRange,
        PostingVisibility visibility)
    {
        var changed = new List<string>();
        if (Title != title) changed.Add(nameof(Title));
        if (Summary != summary) changed.Add(nameof(Summary));
        if (ContractType != contractType) changed.Add(nameof(ContractType));
        if (EducationLevel != educationLevel) changed.Add(nameof(EducationLevel));
        if (WorkFormat != workFormat) changed.Add(nameof(WorkFormat));
        if (Location != location) changed.Add(nameof(Location));
        if (!RequiredSkills.SequenceEqual(requiredSkills)) changed.Add(nameof(RequiredSkills));
        if (!RequiredLanguages.SequenceEqual(requiredLanguages)) changed.Add(nameof(RequiredLanguages));
        if (Deadline != deadline) changed.Add(nameof(Deadline));
        if (JobLink != jobLink) changed.Add(nameof(JobLink));
        if (SalaryRange != salaryRange) changed.Add(nameof(SalaryRange));
        if (Visibility != visibility) changed.Add(nameof(Visibility));
        return changed;
    }

    private void Touch(DateTime? nowUtc) => UpdatedOnUtc = nowUtc ?? DateTime.UtcNow;

    private void RaiseStatusChanged(PostingStatus from, PostingStatus to, AuditActorKind actorKind, DateTime occurred)
    {
        RaiseDomainEvent(new JobPostingStatusChangedIntegrationEvent(
            Id,
            from.ToString(),
            to.ToString(),
            JobPostingStatusRules.IsSearchable(to),
            JobPostingStatusRules.IsAcceptingApplications(to),
            actorKind.ToString(),
            occurred));
    }
}

public sealed class EmployerStanding
{
    public Guid EmployerId { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public bool MayPublish => IsVerified && IsActive;

    private EmployerStanding() { }

    public EmployerStanding(Guid employerId, bool isVerified, bool isActive, DateTime updatedOnUtc)
    {
        EmployerId = employerId;
        IsVerified = isVerified;
        IsActive = isActive;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static EmployerStanding Ineligible(Guid employerId) => new(employerId, false, false, DateTime.UtcNow);
}
