using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public enum VerificationOutcome
{
    NotStarted,
    AutoPending,
    AutoPassed,
    AutoFailed,
    ManualPending,
    ManualPassed,
    ManualRejected
}

public enum VerificationMethod
{
    None,
    Automatic,
    Manual
}

public class VerificationState : ValueObject
{
    public VerificationOutcome Outcome { get; }
    public VerificationMethod Method { get; }
    public string? EvidenceRef { get; }
    public string? RejectionReason { get; }
    public DateTime? LastAttemptUtc { get; }

    private VerificationState(VerificationOutcome outcome, VerificationMethod method, string? evidenceRef, string? rejectionReason, DateTime? lastAttemptUtc)
    {
        Outcome = outcome;
        Method = method;
        EvidenceRef = evidenceRef;
        RejectionReason = rejectionReason;
        LastAttemptUtc = lastAttemptUtc;
    }

    public static Result<VerificationState> Create(
        VerificationOutcome outcome, 
        VerificationMethod method, 
        string? evidenceRef = null, 
        string? rejectionReason = null, 
        DateTime? lastAttemptUtc = null)
    {
        if (outcome == VerificationOutcome.ManualRejected && string.IsNullOrWhiteSpace(rejectionReason))
        {
            return Result.Failure<VerificationState>(new Error("VerificationState.RejectionReasonRequired", "Rejection reason is required when manually rejecting verification."));
        }

        return Result.Success(new VerificationState(
            outcome,
            method,
            evidenceRef?.Trim(),
            rejectionReason?.Trim(),
            lastAttemptUtc));
    }

    public static VerificationState Initial() => new(VerificationOutcome.NotStarted, VerificationMethod.None, null, null, null);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Outcome;
        yield return Method;
        if (EvidenceRef != null) yield return EvidenceRef;
        if (RejectionReason != null) yield return RejectionReason;
        if (LastAttemptUtc.HasValue) yield return LastAttemptUtc.Value;
    }
}
