using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubTaxonomyApi : ITaxonomyApi
{
    public Task<Result<CanonicalSkillRef>> MapSkillAsync(
        string rawSkillLabel,
        CancellationToken cancellationToken = default)
    {
        var cleanLabel = rawSkillLabel?.Trim() ?? "Unknown";
        var code = $"SKILL-{cleanLabel.ToUpperInvariant().Replace(" ", "_")}";
        var skillRef = CanonicalSkillRef.Create(code, cleanLabel).Value;
        return Task.FromResult(Result.Success(skillRef));
    }

    public Task<bool> IsValidSkillCodeAsync(
        string taxonomyCode,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(taxonomyCode));
    }
}
