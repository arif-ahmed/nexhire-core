using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;

public interface IResumeParser
{
    Task<Result<ParsedResumeData>> ParseAsync(FileReference file, CancellationToken cancellationToken = default);
}
