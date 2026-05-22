using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;

public class JobIndexEntry : AggregateRoot<Guid>
{
    public Guid EmployerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string[] Skills { get; private set; } = [];
    public string? EducationRequirement { get; private set; }
    public int? ExperienceYears { get; private set; }
    public GeoLocation Location { get; private set; } = null!;
    public EmploymentType EmploymentType { get; private set; }
    public WorkFormat WorkFormat { get; private set; }
    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public string? SalaryCurrency { get; private set; }
    public string? SectorIndustry { get; private set; }
    public DateTime PostedOnUtc { get; private set; }
    public DateTime? ApplicationDeadlineUtc { get; private set; }
    public long SourcePostingVersion { get; private set; }
    public DateTime IndexedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private JobIndexEntry() { }

    public static Result<JobIndexEntry> Project(
        Guid postingId,
        Guid employerId,
        string title,
        string summary,
        string companyName,
        string[] skills,
        string? educationRequirement,
        int? experienceYears,
        string locationDistrict,
        string? locationCity,
        double? locationLatitude,
        double? locationLongitude,
        EmploymentType employmentType,
        WorkFormat workFormat,
        decimal? salaryMin,
        decimal? salaryMax,
        string? salaryCurrency,
        string? sectorIndustry,
        DateTime postedOnUtc,
        DateTime? applicationDeadlineUtc,
        long sourceVersion,
        DateTime nowUtc)
    {
        var locationResult = GeoLocation.Create(locationDistrict, locationCity, locationLatitude, locationLongitude);
        if (locationResult.IsFailure)
            return Result.Failure<JobIndexEntry>(locationResult.Error);

        var entry = new JobIndexEntry
        {
            Id = postingId,
            EmployerId = employerId,
            Title = title,
            Summary = summary,
            CompanyName = companyName,
            Skills = skills,
            EducationRequirement = educationRequirement,
            ExperienceYears = experienceYears,
            Location = locationResult.Value,
            EmploymentType = employmentType,
            WorkFormat = workFormat,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            SalaryCurrency = salaryCurrency,
            SectorIndustry = sectorIndustry,
            PostedOnUtc = postedOnUtc,
            ApplicationDeadlineUtc = applicationDeadlineUtc,
            SourcePostingVersion = sourceVersion,
            IndexedOnUtc = nowUtc,
            UpdatedOnUtc = nowUtc
        };

        entry.RaiseDomainEvent(new JobIndexed(Guid.NewGuid(), postingId, nowUtc));
        return Result.Success(entry);
    }

    public Result ApplyUpdate(
        string? title,
        string? summary,
        string[]? skills,
        string? educationRequirement,
        int? experienceYears,
        string? locationDistrict,
        string? locationCity,
        double? locationLatitude,
        double? locationLongitude,
        EmploymentType? employmentType,
        WorkFormat? workFormat,
        decimal? salaryMin,
        decimal? salaryMax,
        string? salaryCurrency,
        string? sectorIndustry,
        DateTime? applicationDeadlineUtc,
        long sourceVersion)
    {
        if (sourceVersion <= SourcePostingVersion)
            return Result.Success();

        if (title is not null) Title = title;
        if (summary is not null) Summary = summary;
        if (skills is not null) Skills = skills;
        if (educationRequirement is not null) EducationRequirement = educationRequirement;
        if (experienceYears.HasValue) ExperienceYears = experienceYears;
        if (employmentType.HasValue) EmploymentType = employmentType.Value;
        if (workFormat.HasValue) WorkFormat = workFormat.Value;
        if (salaryMin.HasValue) SalaryMin = salaryMin;
        if (salaryMax.HasValue) SalaryMax = salaryMax;
        if (salaryCurrency is not null) SalaryCurrency = salaryCurrency;
        if (sectorIndustry is not null) SectorIndustry = sectorIndustry;
        if (applicationDeadlineUtc.HasValue) ApplicationDeadlineUtc = applicationDeadlineUtc;

        if (locationDistrict is not null)
        {
            var locationResult = GeoLocation.Create(locationDistrict, locationCity, locationLatitude, locationLongitude);
            if (locationResult.IsSuccess)
                Location = locationResult.Value;
        }

        SourcePostingVersion = sourceVersion;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new JobIndexUpdated(Guid.NewGuid(), Id, UpdatedOnUtc));
        return Result.Success();
    }
}
