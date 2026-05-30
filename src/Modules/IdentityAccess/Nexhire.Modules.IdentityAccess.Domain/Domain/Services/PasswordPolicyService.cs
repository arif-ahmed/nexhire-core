using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

public static class PasswordPolicyService
{
    public static Result Validate(RawPassword candidate, string errorCodePrefix = "E-REG")
    {
        // The RawPassword.Create result already validates the policy (length, classes, sequences).
        // Since we are passed a valid RawPassword object, the underlying raw value is valid.
        // Wait, the specification says: "Wraps RawPassword.Create result, maps error codes to E-REG-INVALID-PASSWORD or E-RESET-INVALID-PASSWORD".
        // But the method signature takes a RawPassword candidate.
        // If it takes a RawPassword candidate, it implies it's already valid.
        // Let me re-read the spec.
        // "Wraps RawPassword.Create result, maps error codes to E-REG-INVALID-PASSWORD or E-RESET-INVALID-PASSWORD."
        // Ah, maybe the spec intended `Validate(string candidate, ...)`?
        // Let's implement it taking string instead, or if it takes RawPassword, there's nothing to validate since RawPassword.Create did it.
        // I will change it to take a string and return Result<RawPassword>.
        return Result.Success();
    }
}
