using Nexhire.Modules.EmployerProfiles.Core.Domain.Events;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Services;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

public class EmployerProfile : AggregateRoot<Guid>
{
    private readonly List<CompanyImage> _images = new();
    private readonly List<SupplementaryDocument> _documents = new();

    public Guid UserId { get; private set; }
    public EmployerProfileStatus Status { get; private set; }
    public CompanyName CompanyName { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public MobileNumber Mobile { get; private set; } = null!;
    public CompanyIdentifier CompanyIdentifier { get; private set; } = null!;
    
    // L2 Optional fields
    public WebsiteUrl? Website { get; private set; }
    public string? Industry { get; private set; }
    public CompanySize? CompanySize { get; private set; }
    public Address? Address { get; private set; }
    public CompanyDescription? Description { get; private set; }

    public FileReference? Logo { get; private set; }
    public IReadOnlyCollection<CompanyImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<SupplementaryDocument> Documents => _documents.AsReadOnly();

    public VerificationState Verification { get; private set; } = null!;
    public ProfileCompleteness Completeness { get; private set; } = null!;
    public EmployerProfileStatus? StatusBeforeSuspend { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    public bool IsVerified => Status == EmployerProfileStatus.Verified;

    private EmployerProfile(
        Guid id,
        Guid userId,
        CompanyName companyName,
        EmailAddress email,
        MobileNumber mobile,
        CompanyIdentifier companyIdentifier) : base(id)
    {
        UserId = userId;
        CompanyName = companyName;
        Email = email;
        Mobile = mobile;
        CompanyIdentifier = companyIdentifier;
        Status = EmployerProfileStatus.PendingActivation;
        Verification = VerificationState.Initial();
        
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
        
        RecalculateCompleteness();
    }

    private EmployerProfile()
    {
        // Required by EF Core
    }

    public static EmployerProfile Register(
        Guid id,
        Guid userId,
        CompanyName companyName,
        EmailAddress email,
        MobileNumber mobile,
        CompanyIdentifier companyIdentifier)
    {
        var profile = new EmployerProfile(id, userId, companyName, email, mobile, companyIdentifier);

        profile.RaiseDomainEvent(new EmployerRegisteredIntegrationEvent(
            Guid.NewGuid(),
            profile.Id,
            profile.UserId,
            profile.CompanyName.Value,
            profile.CreatedOnUtc,
            profile.CreatedOnUtc));

        return profile;
    }

    public Result Activate()
    {
        if (Status == EmployerProfileStatus.PendingVerification || 
            Status == EmployerProfileStatus.PendingManualVerification ||
            Status == EmployerProfileStatus.Verified ||
            Status == EmployerProfileStatus.Rejected)
        {
            return Result.Success(); // Idempotent
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.PendingVerification);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        Status = EmployerProfileStatus.PendingVerification;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileActivated(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result CompleteLevel2(
        WebsiteUrl website,
        string industry,
        CompanySize companySize,
        Address address,
        CompanyDescription description)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.Suspended", "Cannot complete Level 2 details while suspended."));
        }
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("EmployerProfile.Deactivated", "Cannot complete Level 2 details while deactivated."));
        }
        if (Status == EmployerProfileStatus.PendingActivation)
        {
            return Result.Failure(new Error("EmployerProfile.NotActivated", "Cannot complete Level 2 details before account activation."));
        }

        if (string.IsNullOrWhiteSpace(industry))
        {
            return Result.Failure(new Error("EmployerProfile.InvalidIndustry", "Industry is required."));
        }

        Website = website;
        Industry = industry.Trim();
        CompanySize = companySize;
        Address = address;
        Description = description;

        UpdatedOnUtc = DateTime.UtcNow;
        RecalculateCompleteness();

        RaiseDomainEvent(new EmployerProfileUpdatedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            new List<string> { nameof(Website), nameof(Industry), nameof(CompanySize), nameof(Address), nameof(Description) },
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result BeginAutomaticVerification(string registryRef)
    {
        if (Status == EmployerProfileStatus.PendingActivation)
        {
            return Result.Failure(new Error("EmployerProfile.NotActivated", "Verification can only begin once the account is activated."));
        }

        if (!Completeness.Level2Complete)
        {
            return Result.Failure(new Error("EmployerProfile.Level2Incomplete", "Level 2 profile details must be complete before verification."));
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.PendingVerification);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.AutoPending);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.PendingVerification;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.AutoPending, VerificationMethod.Automatic, null, null, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new EmployerVerificationRequestedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            registryRef,
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RecordAutomaticVerificationPassed(string evidenceRef)
    {
        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.Verified);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.AutoPassed);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.Verified;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.AutoPassed, VerificationMethod.Automatic, evidenceRef, null, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new EmployerVerifiedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            now,
            evidenceRef,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RecordAutomaticVerificationFailed()
    {
        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.PendingManualVerification);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.AutoFailed);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.PendingManualVerification;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.AutoFailed, VerificationMethod.Automatic, null, null, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        // Transition verification outcome to ManualPending for the manual verification state
        var manualPendingResult = VerificationState.Create(VerificationOutcome.ManualPending, VerificationMethod.Manual, null, null, now);
        if (manualPendingResult.IsSuccess)
        {
            Verification = manualPendingResult.Value;
        }

        RaiseDomainEvent(new EmployerManualVerificationRequiredIntegrationEvent(
            Guid.NewGuid(),
            Id,
            "auto-verification-failed",
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result ApproveManualVerification(Guid byAdminId, string evidenceRef)
    {
        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.Verified);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.ManualPassed);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.Verified;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.ManualPassed, VerificationMethod.Manual, evidenceRef + $" (Approved by: {byAdminId})", null, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new EmployerVerifiedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            now,
            evidenceRef,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RejectManualVerification(Guid byAdminId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("EmployerProfile.RejectionReasonRequired", "Rejection reason is required."));
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.Rejected);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.ManualRejected);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.Rejected;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.ManualRejected, VerificationMethod.Manual, $"Rejected by: {byAdminId}", reason, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new EmployerVerificationFailedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            reason,
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result ResubmitForVerification()
    {
        if (Status != EmployerProfileStatus.Rejected)
        {
            return Result.Failure(new Error("EmployerProfile.NotRejected", "Only rejected profiles can be resubmitted."));
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.PendingManualVerification);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        var outcomeResult = VerificationStateMachine.EnsureVerificationOutcomeAllowed(Verification.Outcome, VerificationOutcome.ManualPending);
        if (outcomeResult.IsFailure)
        {
            return outcomeResult;
        }

        Status = EmployerProfileStatus.PendingManualVerification;
        
        var now = DateTime.UtcNow;
        var verificationResult = VerificationState.Create(VerificationOutcome.ManualPending, VerificationMethod.Manual, null, null, now);
        if (verificationResult.IsFailure)
        {
            return Result.Failure(verificationResult.Error);
        }

        Verification = verificationResult.Value;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new EmployerManualVerificationRequiredIntegrationEvent(
            Guid.NewGuid(),
            Id,
            "resubmitted",
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result UpdateCompanyInformation(
        CompanyName? companyName = null,
        WebsiteUrl? website = null,
        string? industry = null,
        CompanySize? companySize = null,
        Address? address = null,
        CompanyDescription? description = null)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.Suspended", "Cannot update company details while suspended."));
        }
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("EmployerProfile.Deactivated", "Cannot update company details while deactivated."));
        }
        if (Status == EmployerProfileStatus.PendingActivation)
        {
            return Result.Failure(new Error("EmployerProfile.NotActivated", "Cannot update company details before account activation."));
        }

        var changedFields = new List<string>();

        if (companyName != null)
        {
            CompanyName = companyName;
            changedFields.Add(nameof(CompanyName));
        }

        if (website != null)
        {
            Website = website;
            changedFields.Add(nameof(Website));
        }

        if (industry != null)
        {
            Industry = industry.Trim();
            changedFields.Add(nameof(Industry));
        }

        if (companySize != null)
        {
            CompanySize = companySize;
            changedFields.Add(nameof(CompanySize));
        }

        if (address != null)
        {
            Address = address;
            changedFields.Add(nameof(Address));
        }

        if (description != null)
        {
            Description = description;
            changedFields.Add(nameof(Description));
        }

        if (changedFields.Any())
        {
            UpdatedOnUtc = DateTime.UtcNow;
            RecalculateCompleteness();

            RaiseDomainEvent(new EmployerProfileUpdatedIntegrationEvent(
                Guid.NewGuid(),
                Id,
                changedFields,
                UpdatedOnUtc,
                UpdatedOnUtc));
        }

        return Result.Success();
    }

    public Result SetLogo(FileReference fileReference, VirusScanResult scanResult)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.Suspended", "Cannot set logo while suspended."));
        }
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("EmployerProfile.Deactivated", "Cannot set logo while deactivated."));
        }
        var policyResult = UploadPolicyService.ValidateLogo(fileReference, scanResult);
        if (policyResult.IsFailure)
        {
            return policyResult;
        }

        Logo = fileReference;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileUpdatedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            new List<string> { nameof(Logo) },
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result AddCompanyImage(FileReference fileReference, VirusScanResult scanResult)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.Suspended", "Cannot add company image while suspended."));
        }
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("EmployerProfile.Deactivated", "Cannot add company image while deactivated."));
        }
        var policyResult = UploadPolicyService.ValidateCompanyImage(fileReference, scanResult, _images.Count);
        if (policyResult.IsFailure)
        {
            return policyResult;
        }

        var image = CompanyImage.Create(Guid.NewGuid(), fileReference, scanResult);
        _images.Add(image);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileUpdatedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            new List<string> { nameof(Images) },
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RemoveCompanyImage(Guid companyImageId)
    {
        var image = _images.FirstOrDefault(img => img.Id == companyImageId);
        if (image == null)
        {
            return Result.Failure(new Error("EmployerProfile.ImageNotFound", "Company image not found."));
        }

        _images.Remove(image);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileUpdatedIntegrationEvent(
            Guid.NewGuid(),
            Id,
            new List<string> { nameof(Images) },
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result AddSupplementaryDocument(FileReference fileReference, DocumentKind kind, VirusScanResult scanResult)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.Suspended", "Cannot add supplementary document while suspended."));
        }
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Failure(new Error("EmployerProfile.Deactivated", "Cannot add supplementary document while deactivated."));
        }
        var policyResult = UploadPolicyService.ValidateSupplementaryDocument(fileReference, scanResult, _documents.Count);
        if (policyResult.IsFailure)
        {
            return policyResult;
        }

        var document = SupplementaryDocument.Create(Guid.NewGuid(), fileReference, kind, scanResult);
        _documents.Add(document);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SupplementaryDocumentAdded(
            Guid.NewGuid(),
            Id,
            document.Id,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RemoveSupplementaryDocument(Guid documentId)
    {
        var document = _documents.FirstOrDefault(doc => doc.Id == documentId);
        if (document == null)
        {
            return Result.Failure(new Error("EmployerProfile.DocumentNotFound", "Supplementary document not found."));
        }

        _documents.Remove(document);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SupplementaryDocumentRemoved(
            Guid.NewGuid(),
            Id,
            documentId,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result Suspend(string reason)
    {
        if (Status == EmployerProfileStatus.Suspended)
        {
            return Result.Success(); // Idempotent
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.Suspended);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        StatusBeforeSuspend = Status;
        Status = EmployerProfileStatus.Suspended;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileSuspended(Guid.NewGuid(), Id, reason, UpdatedOnUtc));

        return Result.Success();
    }

    public Result Reinstate()
    {
        if (Status != EmployerProfileStatus.Suspended)
        {
            return Result.Failure(new Error("EmployerProfile.NotSuspended", "Only suspended profiles can be reinstated."));
        }

        var targetStatus = StatusBeforeSuspend ?? EmployerProfileStatus.PendingVerification;
        
        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, targetStatus);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        Status = targetStatus;
        StatusBeforeSuspend = null;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileReinstated(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status == EmployerProfileStatus.Deactivated)
        {
            return Result.Success(); // Idempotent
        }

        var transitionResult = VerificationStateMachine.EnsureTransitionAllowed(Status, EmployerProfileStatus.Deactivated);
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        Status = EmployerProfileStatus.Deactivated;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new EmployerProfileDeactivated(Guid.NewGuid(), Id, UpdatedOnUtc));

        return Result.Success();
    }

    private void RecalculateCompleteness()
    {
        Completeness = EmployerProfileCompletenessService.Evaluate(
            CompanyName,
            Email,
            Mobile,
            CompanyIdentifier,
            Website,
            Industry,
            CompanySize,
            Address,
            Description);
    }
}
