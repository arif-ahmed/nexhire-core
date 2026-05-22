using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Infrastructure.Adapters;

public class StubEmployerAccessApi : IEmployerAccessApi
{
    public Task<bool> CanRecruiterAccessPostingAsync(Guid recruiterId, Guid jobPostingId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<Result<IReadOnlyCollection<Guid>>> GetPostingsForRecruiterAsync(Guid recruiterId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> list = new List<Guid> { Guid.NewGuid() };
        return Task.FromResult(Result.Success(list));
    }
}
