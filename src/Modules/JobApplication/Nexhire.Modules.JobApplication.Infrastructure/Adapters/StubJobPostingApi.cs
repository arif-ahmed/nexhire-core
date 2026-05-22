using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Infrastructure.Adapters;

public class StubJobPostingApi : IJobPostingApi
{
    public Task<Result<PostingApplicabilitySnapshot>> GetApplicabilityAsync(Guid jobPostingId, CancellationToken cancellationToken)
    {
        var snapshot = new PostingApplicabilitySnapshot(
            jobPostingId,
            Guid.NewGuid(), // Mock EmployerId
            "Active",       // Status
            DateTime.UtcNow.AddDays(30) // Deadline in the future
        );
        return Task.FromResult(Result.Success(snapshot));
    }

    public Task<Result<IReadOnlyCollection<PostingSummaryDto>>> GetSummariesAsync(IEnumerable<Guid> jobPostingIds, CancellationToken cancellationToken)
    {
        var summaries = jobPostingIds.Select(id => new PostingSummaryDto(
            id,
            "Senior C# Software Engineer",
            "Nexhire Inc.",
            "Remote, US",
            "$120,000 - $150,000",
            "Active"
        )).ToList();

        return Task.FromResult(Result.Success<IReadOnlyCollection<PostingSummaryDto>>(summaries));
    }
}
