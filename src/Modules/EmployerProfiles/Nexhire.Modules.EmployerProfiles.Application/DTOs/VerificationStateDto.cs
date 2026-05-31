namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record VerificationStateDto(
    string Outcome,
    string Method,
    string? EvidenceRef,
    string? RejectionReason,
    DateTime? LastAttemptUtc);
