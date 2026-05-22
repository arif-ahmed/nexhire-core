using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public enum WorkArrangement
{
    OnSite,
    Hybrid,
    Remote
}

public class JobPreferences : ValueObject
{
    public IReadOnlyCollection<string> JobTypes { get; }
    public IReadOnlyCollection<string> Industries { get; }
    public IReadOnlyCollection<string> Locations { get; }
    public IReadOnlyCollection<WorkArrangement> WorkArrangements { get; }
    public SalaryExpectation? SalaryExpectation { get; }

    private JobPreferences(
        IReadOnlyCollection<string> jobTypes,
        IReadOnlyCollection<string> industries,
        IReadOnlyCollection<string> locations,
        IReadOnlyCollection<WorkArrangement> workArrangements,
        SalaryExpectation? salaryExpectation)
    {
        JobTypes = jobTypes;
        Industries = industries;
        Locations = locations;
        WorkArrangements = workArrangements;
        SalaryExpectation = salaryExpectation;
    }

    public static Result<JobPreferences> Create(
        IEnumerable<string> jobTypes,
        IEnumerable<string> industries,
        IEnumerable<string> locations,
        IEnumerable<WorkArrangement> workArrangements,
        SalaryExpectation? salaryExpectation = null)
    {
        var jobTypesList = jobTypes?.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
        var industriesList = industries?.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
        var locationsList = locations?.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
        var arrangementsList = workArrangements?.Distinct().ToList() ?? new List<WorkArrangement>();

        if (!jobTypesList.Any())
        {
            return Result.Failure<JobPreferences>(new Error("JobPreferences.EmptyJobTypes", "At least one job type must be specified."));
        }

        return Result.Success(new JobPreferences(
            jobTypesList.AsReadOnly(),
            industriesList.AsReadOnly(),
            locationsList.AsReadOnly(),
            arrangementsList.AsReadOnly(),
            salaryExpectation));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var jt in JobTypes) yield return jt;
        foreach (var ind in Industries) yield return ind;
        foreach (var loc in Locations) yield return loc;
        foreach (var arr in WorkArrangements) yield return arr;
        if (SalaryExpectation != null) yield return SalaryExpectation;
    }
}
