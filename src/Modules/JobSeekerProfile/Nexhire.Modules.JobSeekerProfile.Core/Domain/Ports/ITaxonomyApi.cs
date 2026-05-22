using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;

public interface ITaxonomyApi
{
    Task<Result<CanonicalSkillRef>> MapSkillAsync(string rawSkillLabel, CancellationToken cancellationToken = default);
    Task<bool> IsValidSkillCodeAsync(string taxonomyCode, CancellationToken cancellationToken = default);
}
