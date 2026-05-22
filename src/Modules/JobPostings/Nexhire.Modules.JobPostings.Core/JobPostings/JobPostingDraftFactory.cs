using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.JobPostings;

public sealed record BuiltPostingDetails(
    JobTitle Title,
    JobSummary Summary,
    ContractType ContractType,
    EducationLevel EducationLevel,
    WorkFormat WorkFormat,
    EmploymentLocation? Location,
    IReadOnlyCollection<RequiredSkill> RequiredSkills,
    IReadOnlyCollection<LanguageRequirement> RequiredLanguages,
    ApplicationDeadline Deadline,
    JobPostingLink? JobLink,
    SalaryRange? SalaryRange,
    PostingVisibility Visibility);

public static class JobPostingDraftFactory
{
    public static async Task<Result<BuiltPostingDetails>> BuildAsync(JobPostingDraftDto draft, ITaxonomyApi taxonomyApi, CancellationToken cancellationToken)
    {
        var title = JobTitle.Create(draft.Title);
        if (title.IsFailure) return Result.Failure<BuiltPostingDetails>(title.Error);
        var summary = JobSummary.Create(draft.Summary);
        if (summary.IsFailure) return Result.Failure<BuiltPostingDetails>(summary.Error);

        EmploymentLocation? location = null;
        if (draft.Location is not null)
        {
            var locationResult = EmploymentLocation.Create(draft.Location.Line1, draft.Location.City, draft.Location.District, draft.Location.Country);
            if (locationResult.IsFailure) return Result.Failure<BuiltPostingDetails>(locationResult.Error);
            location = locationResult.Value;
        }

        var skills = new List<RequiredSkill>();
        foreach (var input in draft.RequiredSkills)
        {
            var canonical = await taxonomyApi.CanonicalizeSkillAsync(input.RawLabelOrCode, cancellationToken);
            if (canonical.IsFailure) return Result.Failure<BuiltPostingDetails>(canonical.Error);
            var skill = RequiredSkill.Create(canonical.Value, input.RawLabelOrCode, input.Importance);
            if (skill.IsFailure) return Result.Failure<BuiltPostingDetails>(skill.Error);
            skills.Add(skill.Value);
        }

        var languages = new List<LanguageRequirement>();
        foreach (var input in draft.RequiredLanguages)
        {
            var language = LanguageRequirement.Create(input.Language, input.Proficiency);
            if (language.IsFailure) return Result.Failure<BuiltPostingDetails>(language.Error);
            languages.Add(language.Value);
        }

        var deadline = ApplicationDeadline.Create(draft.DeadlineUtc, draft.AutoCloseEnabled);
        if (deadline.IsFailure) return Result.Failure<BuiltPostingDetails>(deadline.Error);

        JobPostingLink? jobLink = null;
        if (!string.IsNullOrWhiteSpace(draft.JobLink))
        {
            var link = JobPostingLink.Create(draft.JobLink);
            if (link.IsFailure) return Result.Failure<BuiltPostingDetails>(link.Error);
            jobLink = link.Value;
        }

        SalaryRange? salaryRange = null;
        if (draft.SalaryRange is not null)
        {
            var salary = SalaryRange.Create(draft.SalaryRange.Min, draft.SalaryRange.Max, draft.SalaryRange.Currency, draft.SalaryRange.Period);
            if (salary.IsFailure) return Result.Failure<BuiltPostingDetails>(salary.Error);
            salaryRange = salary.Value;
        }

        var visibility = JobPostingMappers.ToVisibility(draft.Visibility);
        if (visibility.IsFailure) return Result.Failure<BuiltPostingDetails>(visibility.Error);

        return Result.Success(new BuiltPostingDetails(
            title.Value,
            summary.Value,
            draft.ContractType,
            draft.EducationLevel,
            draft.WorkFormat,
            location,
            skills,
            languages,
            deadline.Value,
            jobLink,
            salaryRange,
            visibility.Value));
    }
}
