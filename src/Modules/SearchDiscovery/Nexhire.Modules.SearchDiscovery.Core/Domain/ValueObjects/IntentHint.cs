using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class IntentHint : ValueObject
{
    public WorkFormat? WorkFormat { get; }
    public IReadOnlyCollection<string> Industries { get; }
    public IReadOnlyCollection<string> SkillTerms { get; }
    public string? LocationTerm { get; }

    private IntentHint(
        WorkFormat? workFormat,
        IReadOnlyCollection<string> industries,
        IReadOnlyCollection<string> skillTerms,
        string? locationTerm)
    {
        WorkFormat = workFormat;
        Industries = industries;
        SkillTerms = skillTerms;
        LocationTerm = locationTerm;
    }

    public static Result<IntentHint> Create(
        WorkFormat? workFormat = null,
        IReadOnlyCollection<string>? industries = null,
        IReadOnlyCollection<string>? skillTerms = null,
        string? locationTerm = null)
    {
        var hasAny = workFormat.HasValue
            || (industries is { Count: > 0 })
            || (skillTerms is { Count: > 0 })
            || !string.IsNullOrWhiteSpace(locationTerm);

        if (!hasAny)
            return Result.Failure<IntentHint>(new Error("IntentHint.Empty", "At least one hint field must be provided."));

        return Result.Success(new IntentHint(
            workFormat,
            industries ?? [],
            skillTerms ?? [],
            string.IsNullOrWhiteSpace(locationTerm) ? null : locationTerm.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return WorkFormat ?? (object?)null!;
        yield return Industries;
        yield return SkillTerms;
        yield return LocationTerm ?? string.Empty;
    }
}
