using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.Domain.Ports;

public interface ITaxonomyApi
{
    Task<Result<CanonicalSkillRef>> CanonicalizeSkillAsync(string rawLabelOrCode, CancellationToken cancellationToken);
    Task<bool> IsValidSkillCodeAsync(string taxonomyCode, CancellationToken cancellationToken);
}

public interface IAuditTrailExporter
{
    Task<Result<ExportedAuditTrail>> ExportAsync(Guid jobPostingId, string format, CancellationToken cancellationToken);
}

public sealed record ExportedAuditTrail(string FileName, string ContentType, byte[] Content);
