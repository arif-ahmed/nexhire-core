using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;

public static class ResumeToProfileMerger
{
    public static Result MergeSelectedFields(
        Aggregates.JobSeekerProfile profile,
        ParsedResumeData parsed,
        IReadOnlyCollection<string> selectedFieldKeys,
        Func<string, Result<CanonicalSkillRef>> mapSkill)
    {
        if (profile == null)
        {
            return Result.Failure(new Error("Merge.NullProfile", "Profile cannot be null."));
        }

        if (parsed == null)
        {
            return Result.Failure(new Error("Merge.NullParsedData", "Parsed resume data cannot be null."));
        }

        var selectedKeys = new HashSet<string>(selectedFieldKeys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var successCount = 0;

        // 1. Merge Education
        var eduList = parsed.Education.ToList();
        for (int i = 0; i < eduList.Count; i++)
        {
            var edu = eduList[i];
            var specificKey = $"education:{i}";
            var matchKey = $"education:{edu.Degree}:{edu.Institution}";

            if (selectedKeys.Contains("Education") || selectedKeys.Contains(specificKey) || selectedKeys.Contains(matchKey))
            {
                var startDate = edu.StartDate ?? DateTime.UtcNow;
                var periodResult = DateRange.Create(startDate, edu.EndDate);
                if (periodResult.IsFailure)
                {
                    errors.Add($"Education [{edu.Degree} at {edu.Institution}]: {periodResult.Error.Message}");
                    continue;
                }

                var addResult = profile.AddEducation(edu.Degree, edu.Institution, periodResult.Value, null);
                if (addResult.IsFailure)
                {
                    errors.Add($"Education [{edu.Degree} at {edu.Institution}]: {addResult.Error.Message}");
                }
                else
                {
                    successCount++;
                }
            }
        }

        // 2. Merge Experience
        var expList = parsed.Experience.ToList();
        for (int i = 0; i < expList.Count; i++)
        {
            var exp = expList[i];
            var specificKey = $"experience:{i}";
            var matchKey = $"experience:{exp.Company}:{exp.Role}";

            if (selectedKeys.Contains("Experience") || selectedKeys.Contains(specificKey) || selectedKeys.Contains(matchKey))
            {
                var startDate = exp.StartDate ?? DateTime.UtcNow;
                var periodResult = DateRange.Create(startDate, exp.IsCurrent ? null : exp.EndDate);
                if (periodResult.IsFailure)
                {
                    errors.Add($"Experience [{exp.Role} at {exp.Company}]: {periodResult.Error.Message}");
                    continue;
                }

                var addResult = profile.AddExperience(exp.Company, exp.Role, periodResult.Value, exp.IsCurrent, exp.Responsibilities ?? string.Empty);
                if (addResult.IsFailure)
                {
                    errors.Add($"Experience [{exp.Role} at {exp.Company}]: {addResult.Error.Message}");
                }
                else
                {
                    successCount++;
                }
            }
        }

        // 3. Merge Skills
        var skillList = parsed.Skills.ToList();
        for (int i = 0; i < skillList.Count; i++)
        {
            var skill = skillList[i];
            var specificKey = $"skill:{i}";
            var matchKey = $"skill:{skill.RawLabel}";

            if (selectedKeys.Contains("Skills") || selectedKeys.Contains(specificKey) || selectedKeys.Contains(matchKey))
            {
                var mapResult = mapSkill(skill.RawLabel);
                if (mapResult.IsFailure)
                {
                    errors.Add($"Skill [{skill.RawLabel}]: {mapResult.Error.Message}");
                    continue;
                }

                var addResult = profile.AddSkill(
                    mapResult.Value,
                    skill.RawLabel,
                    SkillCategory.Hard,
                    SkillTier.Primary,
                    3); // Default to Medium proficiency

                if (addResult.IsFailure)
                {
                    errors.Add($"Skill [{skill.RawLabel}]: {addResult.Error.Message}");
                }
                else
                {
                    successCount++;
                }
            }
        }

        if (errors.Any())
        {
            var errorMsg = string.Join("; ", errors);
            if (successCount > 0)
            {
                // Partially succeeded
                return Result.Failure(new Error("Merge.PartialFailure", $"Some fields failed to merge: {errorMsg}"));
            }
            return Result.Failure(new Error("Merge.Failed", $"All selected fields failed to merge: {errorMsg}"));
        }

        return Result.Success();
    }
}
