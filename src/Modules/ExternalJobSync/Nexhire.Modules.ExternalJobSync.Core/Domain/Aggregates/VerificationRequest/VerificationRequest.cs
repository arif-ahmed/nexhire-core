using Nexhire.Modules.ExternalJobSync.Core.Domain.Events;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using ResultMonad = Nexhire.Shared.Core.Results.Result;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;

public enum VerificationStatus { Pending, InProgress, Verified, Unverified, Error }

public sealed class VerificationRequest : AggregateRoot<Guid>
{
    public VerificationKind Kind { get; private set; }
    public Guid? SubjectUserId { get; private set; }
    public Guid? SubjectJobSeekerProfileId { get; private set; }
    public Guid? SubjectEmployerId { get; private set; }
    public Registry Registry { get; private set; } = null!;
    public ConsentRecord Consent { get; private set; } = null!;
    public MinimisedRequestPayload? RequestPayload { get; private set; }
    public VerificationStatus Status { get; private set; }
    public VerificationResult? Result { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CachedUntilUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private VerificationRequest() { }

    private VerificationRequest(
        Guid id, 
        VerificationKind kind, 
        Guid? subjectUserId, 
        Guid? subjectJobSeekerProfileId, 
        Guid? subjectEmployerId, 
        Registry registry, 
        ConsentRecord consent, 
        MinimisedRequestPayload minimisedPayload,
        DateTime createdOnUtc) : base(id)
    {
        Kind = kind;
        SubjectUserId = subjectUserId;
        SubjectJobSeekerProfileId = subjectJobSeekerProfileId;
        SubjectEmployerId = subjectEmployerId;
        Registry = registry;
        Consent = consent;
        RequestPayload = minimisedPayload;
        Status = VerificationStatus.Pending;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = createdOnUtc;
    }

    public static Nexhire.Shared.Core.Results.Result<VerificationRequest> StartIdentity(Guid subjectUserId, Registry registry, ConsentRecord consent, MinimisedRequestPayload minimisedPayload)
    {
        if (!consent.Granted)
            return ResultMonad.Failure<VerificationRequest>(new Error("E-GOV-CONSENT-REQUIRED", "Government identity verification requires explicit consent."));
        if (subjectUserId == Guid.Empty)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.UserIdRequired", "Subject User ID is required."));
        if (registry == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.RegistryRequired", "Registry is required."));
        if (minimisedPayload == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.PayloadRequired", "Minimised request payload is required."));

        return ResultMonad.Success(new VerificationRequest(
            Guid.NewGuid(), 
            VerificationKind.Identity, 
            subjectUserId, 
            null, 
            null, 
            registry, 
            consent, 
            minimisedPayload, 
            DateTime.UtcNow));
    }

    public static Nexhire.Shared.Core.Results.Result<VerificationRequest> StartEducation(Guid subjectJobSeekerProfileId, Guid subjectUserId, Registry registry, ConsentRecord consent, MinimisedRequestPayload minimisedPayload)
    {
        if (!consent.Granted)
            return ResultMonad.Failure<VerificationRequest>(new Error("E-GOV-CONSENT-REQUIRED", "Government educational verification requires explicit consent."));
        if (subjectJobSeekerProfileId == Guid.Empty)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.ProfileIdRequired", "Subject Seeker Profile ID is required."));
        if (subjectUserId == Guid.Empty)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.UserIdRequired", "Subject User ID is required."));
        if (registry == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.RegistryRequired", "Registry is required."));
        if (minimisedPayload == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.PayloadRequired", "Minimised request payload is required."));

        return ResultMonad.Success(new VerificationRequest(
            Guid.NewGuid(), 
            VerificationKind.Education, 
            subjectUserId, 
            subjectJobSeekerProfileId, 
            null, 
            registry, 
            consent, 
            minimisedPayload, 
            DateTime.UtcNow));
    }

    public static Nexhire.Shared.Core.Results.Result<VerificationRequest> StartEmployer(Guid subjectEmployerId, Registry registry, ConsentRecord consent, MinimisedRequestPayload minimisedPayload)
    {
        if (!consent.Granted)
            return ResultMonad.Failure<VerificationRequest>(new Error("E-GOV-CONSENT-REQUIRED", "Government employer verification requires explicit consent."));
        if (subjectEmployerId == Guid.Empty)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.EmployerIdRequired", "Subject Employer ID is required."));
        if (registry == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.RegistryRequired", "Registry is required."));
        if (minimisedPayload == null)
            return ResultMonad.Failure<VerificationRequest>(new Error("Verification.PayloadRequired", "Minimised request payload is required."));

        return ResultMonad.Success(new VerificationRequest(
            Guid.NewGuid(), 
            VerificationKind.Employer, 
            null, 
            null, 
            subjectEmployerId, 
            registry, 
            consent, 
            minimisedPayload, 
            DateTime.UtcNow));
    }

    public Nexhire.Shared.Core.Results.Result BeginProcessing()
    {
        if (Status != VerificationStatus.Pending)
            return ResultMonad.Failure(new Error("Verification.InvalidState", "Verification can only begin processing from Pending state."));

        Status = VerificationStatus.InProgress;
        UpdatedOnUtc = DateTime.UtcNow;
        return ResultMonad.Success();
    }

    public Nexhire.Shared.Core.Results.Result RecordVerified(VerificationResult result)
    {
        if (Status != VerificationStatus.InProgress)
            return ResultMonad.Failure(new Error("Verification.NotInProgress", "Can only record verification outcome when in progress."));

        Status = VerificationStatus.Verified;
        Result = result;
        CachedUntilUtc = DateTime.UtcNow.AddMonths(12);
        UpdatedOnUtc = DateTime.UtcNow;

        if (Kind == VerificationKind.Identity && SubjectUserId != null)
        {
            RaiseDomainEvent(new IdentityVerifiedByGovernmentIntegrationEvent(SubjectUserId.Value, Registry.Name, result.RespondedOnUtc, UpdatedOnUtc));
        }
        else if (Kind == VerificationKind.Education && SubjectJobSeekerProfileId != null)
        {
            RaiseDomainEvent(new EducationVerifiedIntegrationEvent(SubjectJobSeekerProfileId.Value, result.CredentialRef ?? string.Empty, result.RespondedOnUtc, UpdatedOnUtc));
        }
        else if (Kind == VerificationKind.Employer && SubjectEmployerId != null)
        {
            RaiseDomainEvent(new EmployerVerifiedByGovernmentIntegrationEvent(SubjectEmployerId.Value, Registry.Name, result.RespondedOnUtc, UpdatedOnUtc));
        }

        return ResultMonad.Success();
    }

    public Nexhire.Shared.Core.Results.Result RecordUnverified(VerificationResult result)
    {
        if (Status != VerificationStatus.InProgress)
            return ResultMonad.Failure(new Error("Verification.NotInProgress", "Can only record verification outcome when in progress."));

        Status = VerificationStatus.Unverified;
        Result = result;
        UpdatedOnUtc = DateTime.UtcNow;

        if (Kind == VerificationKind.Identity && SubjectUserId != null)
        {
            RaiseDomainEvent(new IdentityVerificationFailedIntegrationEvent(SubjectUserId.Value, Registry.Name, "Unverified status returned from government database.", UpdatedOnUtc));
        }

        return ResultMonad.Success();
    }

    public Nexhire.Shared.Core.Results.Result RecordError(string reason)
    {
        if (Status is not (VerificationStatus.Pending or VerificationStatus.InProgress))
            return ResultMonad.Failure(new Error("Verification.TerminalState", "Cannot record error on a terminal verification request."));

        Status = VerificationStatus.Error;
        FailureReason = reason;
        UpdatedOnUtc = DateTime.UtcNow;

        if (Kind == VerificationKind.Identity && SubjectUserId != null)
        {
            RaiseDomainEvent(new IdentityVerificationFailedIntegrationEvent(SubjectUserId.Value, Registry.Name, reason, UpdatedOnUtc));
        }

        return ResultMonad.Success();
    }

    public bool IsCacheValid(DateTime nowUtc)
    {
        return Status == VerificationStatus.Verified && CachedUntilUtc != null && CachedUntilUtc > nowUtc;
    }

    public Nexhire.Shared.Core.Results.Result RevokeForDataDeletion()
    {
        RequestPayload = null; // Tombstoning payload
        Result = null; // Clearing results data for GDPR compliance
        UpdatedOnUtc = DateTime.UtcNow;

        if (SubjectUserId != null)
        {
            RaiseDomainEvent(new GovernmentDataDeleted(SubjectUserId.Value));
        }

        return ResultMonad.Success();
    }
}
