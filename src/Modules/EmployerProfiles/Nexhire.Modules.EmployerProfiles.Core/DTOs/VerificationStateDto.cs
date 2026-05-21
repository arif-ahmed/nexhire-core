namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record VerificationStateDto(
    string Outcome,
    string Method,
    string? EvidenceRef,
    string? RejectionReason,
    DateTime? LastAttemptUtc);
