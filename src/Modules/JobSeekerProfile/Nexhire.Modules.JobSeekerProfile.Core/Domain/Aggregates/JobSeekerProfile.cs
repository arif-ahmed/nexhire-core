using Nexhire.Modules.JobSeekerProfile.Core.Domain.Events;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class JobSeekerProfile : AggregateRoot<Guid>
{
    private readonly List<EducationEntry> _education = new();
    private readonly List<ExperienceEntry> _experience = new();
    private readonly List<ProfileSkill> _skills = new();
    private readonly List<SupplementaryDocument> _documents = new();

    public Guid UserId { get; private set; }
    public ProfileStatus Status { get; private set; }
    public PersonName Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public MobileNumber Mobile { get; private set; } = null!;
    public Gender Gender { get; private set; }

    public IReadOnlyCollection<EducationEntry> Education => _education.AsReadOnly();
    public IReadOnlyCollection<ExperienceEntry> Experience => _experience.AsReadOnly();
    public IReadOnlyCollection<ProfileSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<SupplementaryDocument> Documents => _documents.AsReadOnly();

    public JobPreferences? Preferences { get; private set; }
    public Address? CurrentAddress { get; private set; }
    public Address? PermanentAddress { get; private set; }
    public Money? RecentSalary { get; private set; }
    public ProfileVisibility Visibility { get; private set; }
    public PublicSharingSettings PublicSharing { get; private set; } = null!;
    public VerificationFlags Verification { get; private set; } = null!;
    public bool HasActiveResume { get; private set; }
    public CompletenessScore Completeness { get; private set; } = null!;

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    public bool IsLevel2Complete => _education.Any() || _experience.Any();

    private JobSeekerProfile(
        Guid id,
        Guid userId,
        PersonName name,
        EmailAddress email,
        MobileNumber mobile,
        Gender gender) : base(id)
    {
        UserId = userId;
        Status = ProfileStatus.PendingActivation;
        Name = name;
        Email = email;
        Mobile = mobile;
        Gender = gender;
        Visibility = ProfileVisibility.Private;
        PublicSharing = PublicSharingSettings.CreateDisabled().Value;
        Verification = VerificationFlags.CreateDefault().Value;
        HasActiveResume = false;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
        
        // Initial recompute
        RecomputeCompleteness();
    }

    private JobSeekerProfile()
    {
        // Required by EF Core
    }

    public static Result<JobSeekerProfile> Register(
        Guid id,
        Guid userId,
        PersonName name,
        EmailAddress email,
        MobileNumber mobile,
        Gender gender)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<JobSeekerProfile>(new Error("JobSeekerProfile.InvalidUserId", "UserId cannot be empty."));
        }

        var profile = new JobSeekerProfile(id, userId, name, email, mobile, gender);
        
        profile.RaiseDomainEvent(new JobSeekerRegisteredEvent(
            Guid.NewGuid(),
            profile.Id,
            profile.UserId,
            profile.CreatedOnUtc));

        return Result.Success(profile);
    }

    public Result Activate()
    {
        if (Status == ProfileStatus.Active)
        {
            return Result.Success();
        }

        if (Status == ProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("Profile.ActivateFromDeactivated", "Deactivated profiles cannot be activated directly. Use Reactivate."));
        }

        Status = ProfileStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProfileActivatedEvent(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status != ProfileStatus.Active)
        {
            return Result.Failure(new Error("Profile.DeactivateInvalidStatus", "Only active profiles can be deactivated."));
        }

        Status = ProfileStatus.Deactivated;
        PublicSharing = PublicSharingSettings.CreateDisabled().Value; // Disable public sharing on deactivation
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ProfileDeactivatedEvent(Guid.NewGuid(), Id, UpdatedOnUtc));
        RaiseDomainEvent(new PublicSharingDisabledEvent(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != ProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("Profile.ReactivateInvalidStatus", "Only deactivated profiles can be reactivated."));
        }

        Status = ProfileStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ProfileReactivatedEvent(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result AddEducation(string degree, string institution, DateRange period, decimal? gpa)
    {
        var wasL2Complete = IsLevel2Complete;

        var entryResult = EducationEntry.Create(Guid.NewGuid(), degree, institution, period, gpa);
        if (entryResult.IsFailure)
        {
            return Result.Failure(entryResult.Error);
        }

        _education.Add(entryResult.Value);
        UpdatedOnUtc = DateTime.UtcNow;

        RecomputeCompleteness();
        TriggerLevel2CompletionEventIfFlipped(wasL2Complete);

        return Result.Success();
    }

    public Result UpdateEducation(Guid entryId, string degree, string institution, DateRange period, decimal? gpa)
    {
        var entry = _education.FirstOrDefault(x => x.Id == entryId);
        if (entry == null)
        {
            return Result.Failure(new Error("EducationEntry.NotFound", "Education entry not found."));
        }

        var tempResult = EducationEntry.Create(entryId, degree, institution, period, gpa);
        if (tempResult.IsFailure)
        {
            return Result.Failure(tempResult.Error);
        }

        entry.Update(degree, institution, period, gpa);
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result RemoveEducation(Guid entryId)
    {
        var entry = _education.FirstOrDefault(x => x.Id == entryId);
        if (entry == null)
        {
            return Result.Failure(new Error("EducationEntry.NotFound", "Education entry not found."));
        }

        _education.Remove(entry);
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result AddExperience(string company, string role, DateRange period, bool isCurrent, string responsibilities)
    {
        var wasL2Complete = IsLevel2Complete;

        var entryResult = ExperienceEntry.Create(Guid.NewGuid(), company, role, period, isCurrent, responsibilities);
        if (entryResult.IsFailure)
        {
            return Result.Failure(entryResult.Error);
        }

        _experience.Add(entryResult.Value);
        UpdatedOnUtc = DateTime.UtcNow;

        RecomputeCompleteness();
        TriggerLevel2CompletionEventIfFlipped(wasL2Complete);

        return Result.Success();
    }

    public Result UpdateExperience(Guid entryId, string company, string role, DateRange period, bool isCurrent, string responsibilities)
    {
        var entry = _experience.FirstOrDefault(x => x.Id == entryId);
        if (entry == null)
        {
            return Result.Failure(new Error("ExperienceEntry.NotFound", "Experience entry not found."));
        }

        var tempResult = ExperienceEntry.Create(entryId, company, role, period, isCurrent, responsibilities);
        if (tempResult.IsFailure)
        {
            return Result.Failure(tempResult.Error);
        }

        entry.Update(company, role, period, isCurrent, responsibilities);
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result RemoveExperience(Guid entryId)
    {
        var entry = _experience.FirstOrDefault(x => x.Id == entryId);
        if (entry == null)
        {
            return Result.Failure(new Error("ExperienceEntry.NotFound", "Experience entry not found."));
        }

        _experience.Remove(entry);
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result AddSkill(CanonicalSkillRef canonicalRef, string rawLabel, SkillCategory category, SkillTier tier, int proficiency)
    {
        if (_skills.Any(x => x.CanonicalSkillRef.TaxonomyCode == canonicalRef.TaxonomyCode))
        {
            return Result.Failure(new Error("ProfileSkill.DuplicateSkill", "A skill with this taxonomy code already exists in the profile."));
        }

        var skillResult = ProfileSkill.Create(Guid.NewGuid(), canonicalRef, rawLabel, category, tier, proficiency);
        if (skillResult.IsFailure)
        {
            return Result.Failure(skillResult.Error);
        }

        _skills.Add(skillResult.Value);
        UpdatedOnUtc = DateTime.UtcNow;

        RecomputeCompleteness();

        RaiseDomainEvent(new ProfileSkillsUpdatedEvent(
            Guid.NewGuid(),
            Id,
            new[] { canonicalRef.TaxonomyCode },
            new string[0],
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RemoveSkill(Guid skillId)
    {
        var skill = _skills.FirstOrDefault(x => x.Id == skillId);
        if (skill == null)
        {
            return Result.Failure(new Error("ProfileSkill.NotFound", "Skill not found."));
        }

        _skills.Remove(skill);
        UpdatedOnUtc = DateTime.UtcNow;

        RecomputeCompleteness();

        RaiseDomainEvent(new ProfileSkillsUpdatedEvent(
            Guid.NewGuid(),
            Id,
            new string[0],
            new[] { skill.CanonicalSkillRef.TaxonomyCode },
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result SetPreferences(JobPreferences preferences)
    {
        if (preferences == null)
        {
            return Result.Failure(new Error("Profile.NullPreferences", "Preferences cannot be null."));
        }

        Preferences = preferences;
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result SetAddresses(Address currentAddress, Address? permanentAddress)
    {
        if (currentAddress == null)
        {
            return Result.Failure(new Error("Profile.NullCurrentAddress", "Current address is required."));
        }

        CurrentAddress = currentAddress;
        PermanentAddress = permanentAddress;
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result SetRecentSalary(Money? money)
    {
        RecentSalary = money;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddSupplementaryDocument(FileReference file, DocumentKind kind, VirusScanResult scanResult)
    {
        if (_documents.Count >= 10)
        {
            return Result.Failure(new Error("E-UPLOAD-LIMIT-EXCEEDED", "Maximum limit of 10 supplementary documents has been exceeded."));
        }

        var docResult = SupplementaryDocument.Create(Guid.NewGuid(), file, kind, scanResult);
        if (docResult.IsFailure)
        {
            return Result.Failure(docResult.Error);
        }

        _documents.Add(docResult.Value);
        UpdatedOnUtc = DateTime.UtcNow;

        RecomputeCompleteness();

        RaiseDomainEvent(new SupplementaryDocumentUploadedEvent(Guid.NewGuid(), Id, docResult.Value.Id, kind.ToString(), UpdatedOnUtc));

        return Result.Success();
    }

    public Result RemoveSupplementaryDocument(Guid documentId)
    {
        var doc = _documents.FirstOrDefault(x => x.Id == documentId);
        if (doc == null)
        {
            return Result.Failure(new Error("SupplementaryDocument.NotFound", "Supplementary document not found."));
        }

        _documents.Remove(doc);
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();

        return Result.Success();
    }

    public Result SetVisibility(ProfileVisibility visibility)
    {
        Visibility = visibility;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ProfileVisibilityChangedEvent(Guid.NewGuid(), Id, visibility.ToString(), UpdatedOnUtc));

        return Result.Success();
    }

    public Result EnablePublicSharing(string slug, FileReference qrCodeRef)
    {
        if (Status != ProfileStatus.Active)
        {
            return Result.Failure(new Error("Profile.InactiveForSharing", "Public sharing can only be enabled for active profiles."));
        }

        if (Completeness.Percentage < 100)
        {
            return Result.Failure(new Error("E-SHARE-PROFILE-INCOMPLETE", "Public sharing can only be enabled for 100% complete profiles."));
        }

        var settingsResult = PublicSharingSettings.CreateEnabled(slug, qrCodeRef);
        if (settingsResult.IsFailure)
        {
            return Result.Failure(settingsResult.Error);
        }

        PublicSharing = settingsResult.Value;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PublicSharingEnabledEvent(Guid.NewGuid(), Id, slug, UpdatedOnUtc));

        return Result.Success();
    }

    public Result DisablePublicSharing()
    {
        PublicSharing = PublicSharingSettings.CreateDisabled().Value;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PublicSharingDisabledEvent(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result RegeneratePublicSlug(string newSlug, FileReference newQrCodeRef)
    {
        if (!PublicSharing.Enabled)
        {
            return Result.Failure(new Error("Profile.PublicSharingDisabled", "Cannot regenerate slug when public sharing is disabled."));
        }

        var settingsResult = PublicSharingSettings.CreateEnabled(newSlug, newQrCodeRef);
        if (settingsResult.IsFailure)
        {
            return Result.Failure(settingsResult.Error);
        }

        PublicSharing = settingsResult.Value;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PublicSharingSlugRegeneratedEvent(Guid.NewGuid(), Id, newSlug, UpdatedOnUtc));

        return Result.Success();
    }

    public Result MarkResumeAttached()
    {
        HasActiveResume = true;
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();
        return Result.Success();
    }

    public Result MarkResumeDetached()
    {
        HasActiveResume = false;
        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();
        return Result.Success();
    }

    public void ApplyIdentityVerified()
    {
        Verification = VerificationFlags.Create(true, Verification.EducationVerified, Verification.SelfAttested).Value;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProfileVerificationChangedEvent(Guid.NewGuid(), Id, Verification, UpdatedOnUtc));
    }

    public void ApplyEducationVerified()
    {
        Verification = VerificationFlags.Create(Verification.IdentityVerified, true, Verification.SelfAttested).Value;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProfileVerificationChangedEvent(Guid.NewGuid(), Id, Verification, UpdatedOnUtc));
    }

    public void MarkSelfAttested()
    {
        Verification = VerificationFlags.Create(Verification.IdentityVerified, Verification.EducationVerified, true).Value;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProfileVerificationChangedEvent(Guid.NewGuid(), Id, Verification, UpdatedOnUtc));
    }

    public void RestoreSnapshot(PersonName name, EmailAddress email, MobileNumber mobile, Gender gender, 
                                IEnumerable<EducationEntry> education, IEnumerable<ExperienceEntry> experience, 
                                IEnumerable<ProfileSkill> skills, IEnumerable<SupplementaryDocument> documents,
                                JobPreferences? preferences, Address? currentAddress, Address? permanentAddress,
                                Money? recentSalary, ProfileVisibility visibility, PublicSharingSettings publicSharing,
                                VerificationFlags verification, bool hasActiveResume)
    {
        Name = name;
        Email = email;
        Mobile = mobile;
        Gender = gender;

        _education.Clear();
        _education.AddRange(education);

        _experience.Clear();
        _experience.AddRange(experience);

        _skills.Clear();
        _skills.AddRange(skills);

        _documents.Clear();
        _documents.AddRange(documents);

        Preferences = preferences;
        CurrentAddress = currentAddress;
        PermanentAddress = permanentAddress;
        RecentSalary = recentSalary;
        Visibility = visibility;
        PublicSharing = publicSharing;
        Verification = verification;
        HasActiveResume = hasActiveResume;

        UpdatedOnUtc = DateTime.UtcNow;
        RecomputeCompleteness();
    }

    private void RecomputeCompleteness()
    {
        var oldPercentage = Completeness?.Percentage ?? -1;

        Completeness = ProfileCompletenessCalculator.Calculate(this);

        if (oldPercentage != -1 && Completeness.Percentage != oldPercentage)
        {
            RaiseDomainEvent(new ProfileCompletenessChangedEvent(Guid.NewGuid(), Id, Completeness.Percentage, DateTime.UtcNow));
        }
    }

    private void TriggerLevel2CompletionEventIfFlipped(bool wasL2Complete)
    {
        if (!wasL2Complete && IsLevel2Complete)
        {
            RaiseDomainEvent(new ProfileLevel2CompletedEvent(Guid.NewGuid(), Id, Completeness.Percentage, DateTime.UtcNow));
        }
    }
}
