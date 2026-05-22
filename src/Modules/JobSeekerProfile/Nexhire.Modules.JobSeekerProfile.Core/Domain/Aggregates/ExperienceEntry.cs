using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class ExperienceEntry : Entity<Guid>
{
    public string Company { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public bool IsCurrent { get; private set; }
    public string Responsibilities { get; private set; } = null!;

    private ExperienceEntry(Guid id, string company, string role, DateRange period, bool isCurrent, string responsibilities) : base(id)
    {
        Company = company;
        Role = role;
        Period = period;
        IsCurrent = isCurrent;
        Responsibilities = responsibilities;
    }

    private ExperienceEntry()
    {
        // Required by EF Core
    }

    public static Result<ExperienceEntry> Create(Guid id, string company, string role, DateRange period, bool isCurrent, string responsibilities)
    {
        if (string.IsNullOrWhiteSpace(company))
        {
            return Result.Failure<ExperienceEntry>(new Error("ExperienceEntry.EmptyCompany", "Company name is required."));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return Result.Failure<ExperienceEntry>(new Error("ExperienceEntry.EmptyRole", "Role name is required."));
        }

        if (period == null)
        {
            return Result.Failure<ExperienceEntry>(new Error("ExperienceEntry.NullPeriod", "Period is required."));
        }

        if (isCurrent && period.End.HasValue)
        {
            return Result.Failure<ExperienceEntry>(new Error("ExperienceEntry.InvalidCurrentEndDate", "Current experience entry cannot have an end date."));
        }

        return Result.Success(new ExperienceEntry(id, company.Trim(), role.Trim(), period, isCurrent, responsibilities?.Trim() ?? string.Empty));
    }

    public void Update(string company, string role, DateRange period, bool isCurrent, string responsibilities)
    {
        Company = company.Trim();
        Role = role.Trim();
        Period = period;
        IsCurrent = isCurrent;
        Responsibilities = responsibilities?.Trim() ?? string.Empty;
    }
}
