using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;

public static class ProfileCompletenessCalculator
{
    public static CompletenessScore Calculate(Aggregates.JobSeekerProfile profile)
    {
        var percentage = 30; // L1 is always complete once registered (30%)
        var missing = new List<string>();

        // L2 (50% total, 10% each for 5 milestones)
        if (profile.Education.Any())
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Education");
        }

        if (profile.Experience.Any())
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Experience");
        }

        if (profile.Skills.Any())
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Skills");
        }

        if (profile.Preferences != null)
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Job Preferences");
        }

        if (profile.CurrentAddress != null)
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Current Address");
        }

        // L3 (10% total)
        if (profile.Documents.Any())
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Supplementary Documents");
        }

        // Resume (10% total)
        if (profile.HasActiveResume)
        {
            percentage += 10;
        }
        else
        {
            missing.Add("Active Resume");
        }

        return CompletenessScore.Create(percentage, missing).Value;
    }
}
