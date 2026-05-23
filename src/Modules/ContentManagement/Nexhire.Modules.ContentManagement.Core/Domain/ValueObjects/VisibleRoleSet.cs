using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class VisibleRoleSet : ValueObject
{
    public IReadOnlySet<VisibleRole> Roles { get; }

    private VisibleRoleSet(IReadOnlySet<VisibleRole> roles)
    {
        Roles = roles;
    }

    public static Result<VisibleRoleSet> Create(IEnumerable<VisibleRole> roles)
    {
        var roleSet = roles.ToHashSet();

        if (roleSet.Count == 0)
            return Result.Failure<VisibleRoleSet>(new Error("E-ROLES-EMPTY", "At least one role is required."));

        if (roleSet.Contains(VisibleRole.All) && roleSet.Count > 1)
            return Result.Failure<VisibleRoleSet>(new Error("E-ROLES-ALL-MIXED", "Cannot mix 'All' with specific roles."));

        return Result.Success(new VisibleRoleSet(roleSet));
    }

    public bool Contains(VisibleRole role) => Roles.Contains(VisibleRole.All) || Roles.Contains(role);

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var role in Roles.OrderBy(r => r))
            yield return role;
    }
}
