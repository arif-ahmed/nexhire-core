using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class EducationEntry : Entity<Guid>
{
    public string Degree { get; private set; } = null!;
    public string Institution { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public decimal? Gpa { get; private set; }

    private EducationEntry(Guid id, string degree, string institution, DateRange period, decimal? gpa) : base(id)
    {
        Degree = degree;
        Institution = institution;
        Period = period;
        Gpa = gpa;
    }

    private EducationEntry()
    {
        // Required by EF Core
    }

    public static Result<EducationEntry> Create(Guid id, string degree, string institution, DateRange period, decimal? gpa)
    {
        if (string.IsNullOrWhiteSpace(degree))
        {
            return Result.Failure<EducationEntry>(new Error("EducationEntry.EmptyDegree", "Degree is required."));
        }

        if (string.IsNullOrWhiteSpace(institution))
        {
            return Result.Failure<EducationEntry>(new Error("EducationEntry.EmptyInstitution", "Institution is required."));
        }

        if (period == null)
        {
            return Result.Failure<EducationEntry>(new Error("EducationEntry.NullPeriod", "Period is required."));
        }

        if (gpa.HasValue && (gpa.Value < 0 || gpa.Value > 5.0m)) // Assumes standard GPA scale
        {
            return Result.Failure<EducationEntry>(new Error("EducationEntry.InvalidGpa", "GPA must be between 0.0 and 5.0."));
        }

        return Result.Success(new EducationEntry(id, degree.Trim(), institution.Trim(), period, gpa));
    }

    public void Update(string degree, string institution, DateRange period, decimal? gpa)
    {
        Degree = degree.Trim();
        Institution = institution.Trim();
        Period = period;
        Gpa = gpa;
    }
}
