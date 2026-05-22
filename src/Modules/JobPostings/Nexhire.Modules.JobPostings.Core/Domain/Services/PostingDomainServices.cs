using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.Domain.Services;

public sealed class SchemaOrgStandardizer
{
    public SchemaOrgJobPosting Standardize(JobPosting posting)
    {
        var properties = new Dictionary<string, string>
        {
            ["@type"] = "JobPosting",
            ["title"] = posting.Title.Value,
            ["description"] = posting.Summary.Value,
            ["employmentType"] = posting.ContractType.ToString(),
            ["educationRequirements"] = posting.EducationLevel.ToString(),
            ["validThrough"] = posting.Deadline.DateUtc.ToString("O"),
            ["skills"] = string.Join(",", posting.RequiredSkills.Select(x => x.CanonicalRef.DisplayLabel)),
            ["x-workFormat"] = posting.WorkFormat.ToString(),
            ["x-visibility"] = posting.Visibility.Level.ToString()
        };

        if (posting.Location is not null)
        {
            properties["jobLocation"] = $"{posting.Location.City}, {posting.Location.District}, {posting.Location.Country}";
        }

        if (posting.SalaryRange is not null)
        {
            properties["baseSalary"] = $"{posting.SalaryRange.Min}-{posting.SalaryRange.Max} {posting.SalaryRange.Currency} {posting.SalaryRange.Period}";
        }

        var violations = new List<string>();
        foreach (var required in new[] { "title", "description", "employmentType", "validThrough", "skills" })
        {
            if (!properties.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
            {
                violations.Add(required);
            }
        }

        if (posting.WorkFormat == WorkFormat.Physical && posting.Location is null)
        {
            violations.Add("jobLocation");
        }

        return SchemaOrgJobPosting.Create(properties, violations);
    }
}

public sealed class PostingExpirationPolicy
{
    public bool ShouldExpire(JobPosting posting, DateTime nowUtc) =>
        posting.Status is PostingStatus.Active or PostingStatus.Paused && posting.Deadline.DateUtc <= nowUtc;

    public bool IsApproachingExpiry(JobPosting posting, DateTime nowUtc, TimeSpan threshold) =>
        posting.Status is PostingStatus.Active or PostingStatus.Paused &&
        posting.Deadline.DateUtc > nowUtc &&
        posting.Deadline.DateUtc <= nowUtc.Add(threshold);
}

public sealed class JobPostingRenewalService
{
    public Result<JobPosting> Renew(JobPosting expiredPosting, ApplicationDeadline newDeadline, DateTime? nowUtc = null) =>
        JobPosting.RenewFrom(expiredPosting, newDeadline, nowUtc);
}
