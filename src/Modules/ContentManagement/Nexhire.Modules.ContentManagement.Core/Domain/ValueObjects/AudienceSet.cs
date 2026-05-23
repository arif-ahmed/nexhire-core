using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class AudienceSet : ValueObject
{
    public IReadOnlySet<Audience> Audiences { get; }

    private AudienceSet(IReadOnlySet<Audience> audiences)
    {
        Audiences = audiences;
    }

    public static Result<AudienceSet> Create(IEnumerable<Audience> audiences)
    {
        var set = audiences.ToHashSet();

        if (set.Count == 0)
            return Result.Failure<AudienceSet>(new Error("E-AUDIENCE-EMPTY", "At least one audience is required."));

        return Result.Success(new AudienceSet(set));
    }

    public bool Contains(Audience audience) => Audiences.Contains(audience);

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var a in Audiences.OrderBy(a => a))
            yield return a;
    }
}
