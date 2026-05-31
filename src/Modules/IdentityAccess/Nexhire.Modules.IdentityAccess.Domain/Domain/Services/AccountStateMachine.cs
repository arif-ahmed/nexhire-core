using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

public static class AccountStateMachine
{
    public static Result EnsureTransitionAllowed(AccountStatus from, AccountStatus to)
    {
        bool allowed = (from, to) switch
        {
            (AccountStatus.PendingActivation, AccountStatus.Active) => true,
            (AccountStatus.PendingActivation, AccountStatus.Suspended) => true,
            (AccountStatus.Active, AccountStatus.Suspended) => true,
            (AccountStatus.Suspended, AccountStatus.Active) => true,
            (AccountStatus.Active, AccountStatus.Deactivated) => true,
            (AccountStatus.Deactivated, AccountStatus.PendingActivation) => true,
            (AccountStatus.Deactivated, AccountStatus.Active) => true,
            _ => false
        };

        if (!allowed)
            return Result.Failure(new Error("Account.InvalidTransition", $"Cannot transition from {from} to {to}."));

        return Result.Success();
    }
}
