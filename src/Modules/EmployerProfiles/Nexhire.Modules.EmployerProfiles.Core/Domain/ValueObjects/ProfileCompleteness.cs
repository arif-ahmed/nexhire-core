using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class ProfileCompleteness : ValueObject
{
    public bool Level1Complete { get; }
    public bool Level2Complete { get; }

    private ProfileCompleteness(bool level1Complete, bool level2Complete)
    {
        Level1Complete = level1Complete;
        Level2Complete = level2Complete;
    }

    public static Result<ProfileCompleteness> Create(bool level1Complete, bool level2Complete)
    {
        return Result.Success(new ProfileCompleteness(level1Complete, level2Complete));
    }

    public static ProfileCompleteness Initial() => new(true, false);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Level1Complete;
        yield return Level2Complete;
    }
}
