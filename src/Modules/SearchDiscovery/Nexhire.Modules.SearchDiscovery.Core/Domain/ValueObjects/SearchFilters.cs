using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class SearchFilters : ValueObject
{
    public GeoLocation? Location { get; }
    public int? RadiusKm { get; }
    public decimal? SalaryMin { get; }
    public decimal? SalaryMax { get; }
    public IReadOnlyCollection<EmploymentType> EmploymentTypes { get; }
    public IReadOnlyCollection<WorkFormat> WorkFormats { get; }
    public DateTime? DatePostedFrom { get; }
    public DateTime? DatePostedTo { get; }
    public DateTime? DeadlineBefore { get; }
    public IReadOnlyCollection<string> RequiredSkills { get; }
    public string? EducationLevel { get; }
    public int? MinExperienceYears { get; }
    public string? SectorIndustry { get; }

    private SearchFilters(
        GeoLocation? location, int? radiusKm,
        decimal? salaryMin, decimal? salaryMax,
        IReadOnlyCollection<EmploymentType> employmentTypes,
        IReadOnlyCollection<WorkFormat> workFormats,
        DateTime? datePostedFrom, DateTime? datePostedTo,
        DateTime? deadlineBefore,
        IReadOnlyCollection<string> requiredSkills,
        string? educationLevel, int? minExperienceYears,
        string? sectorIndustry)
    {
        Location = location;
        RadiusKm = radiusKm;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        EmploymentTypes = employmentTypes;
        WorkFormats = workFormats;
        DatePostedFrom = datePostedFrom;
        DatePostedTo = datePostedTo;
        DeadlineBefore = deadlineBefore;
        RequiredSkills = requiredSkills;
        EducationLevel = educationLevel;
        MinExperienceYears = minExperienceYears;
        SectorIndustry = sectorIndustry;
    }

    public static Result<SearchFilters> Create(
        GeoLocation? location = null,
        int? radiusKm = null,
        decimal? salaryMin = null,
        decimal? salaryMax = null,
        IReadOnlyCollection<EmploymentType>? employmentTypes = null,
        IReadOnlyCollection<WorkFormat>? workFormats = null,
        DateTime? datePostedFrom = null,
        DateTime? datePostedTo = null,
        DateTime? deadlineBefore = null,
        IReadOnlyCollection<string>? requiredSkills = null,
        string? educationLevel = null,
        int? minExperienceYears = null,
        string? sectorIndustry = null)
    {
        if (salaryMin.HasValue && salaryMax.HasValue && salaryMin > salaryMax)
            return Result.Failure<SearchFilters>(new Error("SearchFilters.InvalidSalaryRange", "Salary min cannot exceed max."));

        if (datePostedFrom.HasValue && datePostedTo.HasValue && datePostedFrom > datePostedTo)
            return Result.Failure<SearchFilters>(new Error("SearchFilters.InvalidDateRange", "Date posted from cannot exceed to."));

        if (radiusKm.HasValue && location is null)
            return Result.Failure<SearchFilters>(new Error("SearchFilters.RadiusWithoutLocation", "Radius requires a location."));

        return Result.Success(new SearchFilters(
            location, radiusKm,
            salaryMin, salaryMax,
            employmentTypes ?? [],
            workFormats ?? [],
            datePostedFrom, datePostedTo,
            deadlineBefore,
            requiredSkills ?? [],
            educationLevel, minExperienceYears,
            sectorIndustry));
    }

    public bool HasAnyFilter =>
        Location is not null
        || SalaryMin.HasValue
        || SalaryMax.HasValue
        || EmploymentTypes.Count > 0
        || WorkFormats.Count > 0
        || DatePostedFrom.HasValue
        || DatePostedTo.HasValue
        || DeadlineBefore.HasValue
        || RequiredSkills.Count > 0
        || EducationLevel is not null
        || MinExperienceYears.HasValue
        || SectorIndustry is not null;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Location ?? (object?)null!;
        yield return RadiusKm ?? 0;
        yield return SalaryMin ?? 0;
        yield return SalaryMax ?? 0;
        yield return DatePostedFrom ?? DateTime.MinValue;
        yield return DatePostedTo ?? DateTime.MinValue;
        yield return DeadlineBefore ?? DateTime.MinValue;
        yield return EducationLevel ?? string.Empty;
        yield return MinExperienceYears ?? 0;
        yield return SectorIndustry ?? string.Empty;
    }
}
