using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class VerificationFlags : ValueObject
{
    public bool IdentityVerified { get; }
    public bool EducationVerified { get; }
    public bool SelfAttested { get; }

    private VerificationFlags(bool identityVerified, bool educationVerified, bool selfAttested)
    {
        IdentityVerified = identityVerified;
        EducationVerified = educationVerified;
        SelfAttested = selfAttested;
    }

    public static Result<VerificationFlags> Create(bool identityVerified, bool educationVerified, bool selfAttested)
    {
        return Result.Success(new VerificationFlags(identityVerified, educationVerified, selfAttested));
    }

    public static Result<VerificationFlags> CreateDefault()
    {
        return Result.Success(new VerificationFlags(false, false, false));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IdentityVerified;
        yield return EducationVerified;
        yield return SelfAttested;
    }
}
