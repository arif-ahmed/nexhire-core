using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Services;

public static class SearchCriteriaInterpreter
{
    public static SearchCriteria Apply(SearchCriteria baseCriteria, IntentHint? hint)
    {
        if (hint is null)
            return baseCriteria;

        var existingFilters = baseCriteria.Filters;
        var workFormats = existingFilters.WorkFormats.ToList();
        var requiredSkills = existingFilters.RequiredSkills.ToList();

        if (hint.WorkFormat.HasValue && !workFormats.Contains(hint.WorkFormat.Value))
            workFormats.Add(hint.WorkFormat.Value);

        foreach (var skill in hint.SkillTerms)
            if (!requiredSkills.Contains(skill, StringComparer.OrdinalIgnoreCase))
                requiredSkills.Add(skill);

        var location = existingFilters.Location;
        if (location is null && !string.IsNullOrWhiteSpace(hint.LocationTerm))
        {
            var locResult = GeoLocation.Create(hint.LocationTerm);
            if (locResult.IsSuccess)
                location = locResult.Value;
        }

        var mergedFilters = SearchFilters.Create(
            location: location,
            radiusKm: existingFilters.RadiusKm,
            salaryMin: existingFilters.SalaryMin,
            salaryMax: existingFilters.SalaryMax,
            employmentTypes: existingFilters.EmploymentTypes,
            workFormats: workFormats,
            datePostedFrom: existingFilters.DatePostedFrom,
            datePostedTo: existingFilters.DatePostedTo,
            deadlineBefore: existingFilters.DeadlineBefore,
            requiredSkills: requiredSkills,
            educationLevel: existingFilters.EducationLevel,
            minExperienceYears: existingFilters.MinExperienceYears,
            sectorIndustry: existingFilters.SectorIndustry
        ).Value;

        return SearchCriteria.Create(
            keyword: baseCriteria.Keyword,
            filters: mergedFilters,
            sort: baseCriteria.Sort,
            page: baseCriteria.Page,
            pageSize: baseCriteria.PageSize,
            allowEmptyForPersistence: true
        ).Value;
    }
}
