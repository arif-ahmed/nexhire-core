using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record EmployerProfileDto(
    Guid Id,
    Guid UserId,
    string Status,
    string CompanyName,
    string Email,
    string Mobile,
    string CompanyIdentifier,
    string? Website,
    string? Industry,
    string? CompanySize,
    AddressDto? Address,
    string? Description,
    FileReference? Logo,
    IReadOnlyCollection<CompanyImageDto> Images,
    IReadOnlyCollection<SupplementaryDocumentDto> Documents,
    VerificationStateDto Verification,
    bool Level1Complete,
    bool Level2Complete);

public record CompanyImageDto(Guid Id, FileReference File, DateTime UploadedOnUtc);
public record SupplementaryDocumentDto(Guid Id, FileReference File, string Kind, DateTime UploadedOnUtc);
