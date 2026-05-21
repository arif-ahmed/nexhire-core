using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Services;

public class VerificationStateMachine
{
    public static Result EnsureTransitionAllowed(EmployerProfileStatus from, EmployerProfileStatus to)
    {
        if (from == to)
        {
            return Result.Success();
        }

        if (from == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("StatusTransition.Invalid", "Cannot transition out of Deactivated status."));
        }

        var isAllowed = (from, to) switch
        {
            (EmployerProfileStatus.PendingActivation, EmployerProfileStatus.PendingVerification) => true,

            (EmployerProfileStatus.PendingVerification, EmployerProfileStatus.Verified) => true,
            (EmployerProfileStatus.PendingVerification, EmployerProfileStatus.PendingManualVerification) => true,

            (EmployerProfileStatus.PendingManualVerification, EmployerProfileStatus.Verified) => true,
            (EmployerProfileStatus.PendingManualVerification, EmployerProfileStatus.Rejected) => true,

            (EmployerProfileStatus.Rejected, EmployerProfileStatus.PendingManualVerification) => true,

            (_, EmployerProfileStatus.Suspended) => true,
            (_, EmployerProfileStatus.Deactivated) => true,

            (EmployerProfileStatus.Suspended, _) => true, // Reinstating from suspended to prior status

            _ => false
        };

        if (!isAllowed)
        {
            return Result.Failure(new Error("StatusTransition.Invalid", $"Invalid status transition from {from} to {to}."));
        }

        return Result.Success();
    }

    public static Result EnsureVerificationOutcomeAllowed(VerificationOutcome from, VerificationOutcome to)
    {
        if (from == to)
        {
            return Result.Success();
        }

        var isAllowed = (from, to) switch
        {
            (VerificationOutcome.NotStarted, VerificationOutcome.AutoPending) => true,
            (VerificationOutcome.AutoPending, VerificationOutcome.AutoPassed) => true,
            (VerificationOutcome.AutoPending, VerificationOutcome.AutoFailed) => true,
            
            (VerificationOutcome.AutoFailed, VerificationOutcome.ManualPending) => true,
            (VerificationOutcome.ManualPending, VerificationOutcome.ManualPassed) => true,
            (VerificationOutcome.ManualPending, VerificationOutcome.ManualRejected) => true,

            (VerificationOutcome.ManualRejected, VerificationOutcome.ManualPending) => true, // Resubmission

            _ => false
        };

        if (!isAllowed)
        {
            return Result.Failure(new Error("VerificationOutcome.InvalidTransition", $"Invalid verification outcome transition from {from} to {to}."));
        }

        return Result.Success();
    }
}
