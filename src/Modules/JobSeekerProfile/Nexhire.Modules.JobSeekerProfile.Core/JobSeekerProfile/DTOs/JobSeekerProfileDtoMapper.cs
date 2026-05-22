using System.Linq;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;

public static class JobSeekerProfileDtoMapper
{
    public static JobSeekerProfileDto ToDto(this Domain.Aggregates.JobSeekerProfile profile)
    {
        return new JobSeekerProfileDto(
            profile.Id,
            profile.UserId,
            profile.Status.ToString(),
            profile.Name.First,
            profile.Name.Last,
            profile.Email.Value,
            profile.Mobile.Value,
            profile.Gender.ToString(),
            profile.Education.Select(e => e.ToDto()).ToList().AsReadOnly(),
            profile.Experience.Select(e => e.ToDto()).ToList().AsReadOnly(),
            profile.Skills.Select(s => s.ToDto()).ToList().AsReadOnly(),
            profile.Documents.Select(d => d.ToDto()).ToList().AsReadOnly(),
            profile.Preferences?.ToDto(),
            profile.CurrentAddress?.ToDto(),
            profile.PermanentAddress?.ToDto(),
            profile.RecentSalary?.ToDto(),
            profile.Visibility.ToString(),
            profile.PublicSharing.ToDto(),
            profile.Verification.ToDto(),
            profile.HasActiveResume,
            profile.Completeness.ToDto());
    }

    public static EducationEntryDto ToDto(this EducationEntry e)
    {
        return new EducationEntryDto(
            e.Id,
            e.Degree,
            e.Institution,
            e.Period.Start,
            e.Period.End,
            e.Gpa);
    }

    public static ExperienceEntryDto ToDto(this ExperienceEntry e)
    {
        return new ExperienceEntryDto(
            e.Id,
            e.Company,
            e.Role,
            e.Period.Start,
            e.Period.End,
            e.IsCurrent,
            e.Responsibilities);
    }

    public static ProfileSkillDto ToDto(this ProfileSkill s)
    {
        return new ProfileSkillDto(
            s.Id,
            s.CanonicalSkillRef.TaxonomyCode,
            s.CanonicalSkillRef.DisplayLabel,
            s.RawLabel,
            s.Category.ToString(),
            s.Tier.ToString(),
            s.Proficiency);
    }

    public static SupplementaryDocumentDto ToDto(this SupplementaryDocument d)
    {
        return new SupplementaryDocumentDto(
            d.Id,
            d.File.StorageKey,
            d.File.OriginalFileName,
            d.File.MimeType,
            d.File.SizeBytes,
            d.Kind.ToString(),
            d.ScanResult.Status.ToString(),
            d.UploadedOnUtc);
    }

    public static JobPreferencesDto ToDto(this JobPreferences p)
    {
        return new JobPreferencesDto(
            p.JobTypes.ToList().AsReadOnly(),
            p.Industries.ToList().AsReadOnly(),
            p.Locations.ToList().AsReadOnly(),
            p.WorkArrangements.Select(a => a.ToString()).ToList().AsReadOnly(),
            p.SalaryExpectation?.ToDto());
    }

    public static SalaryExpectationDto ToDto(this SalaryExpectation s)
    {
        return new SalaryExpectationDto(
            s.Min.ToDto(),
            s.Max.ToDto());
    }

    public static MoneyDto ToDto(this Money m)
    {
        return new MoneyDto(m.Amount, m.Currency);
    }

    public static AddressDto ToDto(this Address a)
    {
        return new AddressDto(a.Line1, a.Line2, a.City, a.District, a.Postcode, a.Country);
    }

    public static PublicSharingSettingsDto ToDto(this PublicSharingSettings p)
    {
        return new PublicSharingSettingsDto(
            p.Enabled,
            p.Slug,
            p.QrCodeRef?.StorageKey);
    }

    public static VerificationFlagsDto ToDto(this VerificationFlags v)
    {
        return new VerificationFlagsDto(v.IdentityVerified, v.EducationVerified, v.SelfAttested);
    }

    public static CompletenessScoreDto ToDto(this CompletenessScore c)
    {
        return new CompletenessScoreDto(c.Percentage, c.MissingSections.ToList().AsReadOnly());
    }

    public static ProfileHistoryDto ToDto(this ProfileHistory h)
    {
        return new ProfileHistoryDto(
            h.Id,
            h.JobSeekerProfileId,
            h.Versions.Select(v => v.ToDto()).ToList().AsReadOnly());
    }

    public static ProfileVersionDto ToDto(this ProfileVersion v)
    {
        return new ProfileVersionDto(
            v.Id,
            v.Action.ToString(),
            v.ChangedFields.ToList().AsReadOnly(),
            v.CreatedOnUtc);
    }

    public static ResumeParseStatusDto ToDto(this Resume r)
    {
        return new ResumeParseStatusDto(
            r.Id,
            r.ParseStatus.ToString(),
            r.FailureReason,
            r.ParsedData?.ToDto(),
            r.ParsedOnUtc);
    }

    public static ParsedResumeDataDto ToDto(this ParsedResumeData p)
    {
        var fullName = p.Personal != null ? $"{p.Personal.FirstName} {p.Personal.LastName}".Trim() : null;
        return new ParsedResumeDataDto(
            fullName,
            p.Personal?.Email,
            p.Personal?.Mobile,
            p.Education.Select(e => new ParsedEducationDto(e.Degree, e.Institution, e.StartDate, e.EndDate, null)).ToList().AsReadOnly(),
            p.Experience.Select(e => new ParsedExperienceDto(e.Company, e.Role, e.StartDate, e.EndDate, e.IsCurrent, e.Responsibilities)).ToList().AsReadOnly(),
            p.Skills.Select(s => new ParsedSkillDto(s.RawLabel, s.Confidence.Value, s.Confidence.NeedsVerification)).ToList().AsReadOnly());
    }
}
