using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Services;

public static class SavedSearchMatcher
{
    public static bool Matches(SearchCriteria criteria, JobIndexEntry entry)
    {
        var filters = criteria.Filters;

        if (filters.EmploymentTypes.Count > 0 && !filters.EmploymentTypes.Contains(entry.EmploymentType))
            return false;

        if (filters.WorkFormats.Count > 0 && !filters.WorkFormats.Contains(entry.WorkFormat))
            return false;

        if (filters.RequiredSkills.Count > 0)
        {
            var entrySkills = entry.Skills.Select(s => s.ToLowerInvariant()).ToHashSet();
            var requiredSkills = filters.RequiredSkills.Select(s => s.ToLowerInvariant());
            if (!requiredSkills.All(required => entrySkills.Contains(required)))
                return false;
        }

        if (filters.Location is not null &&
            !entry.Location.District.Equals(filters.Location.District, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filters.SalaryMin.HasValue && entry.SalaryMax.HasValue && entry.SalaryMax < filters.SalaryMin)
            return false;

        if (filters.SalaryMax.HasValue && entry.SalaryMin.HasValue && entry.SalaryMin > filters.SalaryMax)
            return false;

        if (filters.MinExperienceYears.HasValue && entry.ExperienceYears < filters.MinExperienceYears)
            return false;

        if (filters.SectorIndustry is not null &&
            !string.Equals(entry.SectorIndustry, filters.SectorIndustry, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
