using System;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Infrastructure.Adapters;

public class StubMatchRankingPublicApi : IMatchRankingPublicApi
{
    public Task<Result<int?>> GetMatchScoreAsync(Guid jobSeekerId, Guid jobPostingId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success<int?>(85));
    }
}
