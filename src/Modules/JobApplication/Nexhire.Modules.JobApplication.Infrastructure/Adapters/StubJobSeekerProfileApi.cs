using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Infrastructure.Adapters;

public class StubJobSeekerProfileApi : IJobSeekerProfileApi
{
    public Task<bool> IsLevel2CompleteAsync(Guid jobSeekerProfileId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<Result<JobSeekerProfileSnapshotDto>> GetSnapshotAsync(Guid jobSeekerProfileId, CancellationToken cancellationToken)
    {
        var snapshot = new JobSeekerProfileSnapshotDto(
            jobSeekerProfileId,
            jobSeekerProfileId, // Mock UserId same as ProfileId
            "Arif Ahmed",
            "arif.ahmed@example.com",
            "+1234567890",
            "Dallas, TX",
            "M.S. in Computer Science",
            "5 years of experience in .NET Development",
            new List<string> { "C#", ".NET 9", "DDD", "Clean Architecture", "PostgreSQL" },
            true,
            "Public"
        );
        return Task.FromResult(Result.Success(snapshot));
    }

    public Task<Result<bool>> IsResumeUsableAsync(Guid jobSeekerProfileId, Guid resumeDocumentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(true));
    }
}
