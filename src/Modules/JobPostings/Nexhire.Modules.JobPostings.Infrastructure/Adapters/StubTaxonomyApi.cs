using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Infrastructure.Adapters;

public sealed class StubTaxonomyApi : ITaxonomyApi
{
    public Task<Result<CanonicalSkillRef>> CanonicalizeSkillAsync(string rawLabelOrCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawLabelOrCode))
        {
            return Task.FromResult(Result.Failure<CanonicalSkillRef>(new Error("E-POST-INVALID-SKILL-CODE", "Skill label or code is required.")));
        }

        var label = rawLabelOrCode.Trim();
        var code = label.StartsWith("SK-", StringComparison.OrdinalIgnoreCase)
            ? label.ToUpperInvariant()
            : $"SK-{label.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToUpperInvariant()}";

        return Task.FromResult(CanonicalSkillRef.Create(code, label));
    }

    public Task<bool> IsValidSkillCodeAsync(string taxonomyCode, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(taxonomyCode) && taxonomyCode.StartsWith("SK-", StringComparison.OrdinalIgnoreCase));
}
