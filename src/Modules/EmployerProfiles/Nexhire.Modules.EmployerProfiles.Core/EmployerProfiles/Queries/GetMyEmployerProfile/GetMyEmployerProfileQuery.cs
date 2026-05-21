using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMyEmployerProfile;

public record GetMyEmployerProfileQuery(Guid UserId) : IQuery<EmployerProfileDto>;

public class GetMyEmployerProfileQueryHandler : IQueryHandler<GetMyEmployerProfileQuery, EmployerProfileDto>
{
    private readonly IEmployerProfileRepository _repository;

    public GetMyEmployerProfileQueryHandler(IEmployerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<EmployerProfileDto>> Handle(GetMyEmployerProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<EmployerProfileDto>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var addressDto = profile.Address == null ? null : new AddressDto(
            profile.Address.Line1,
            profile.Address.Line2,
            profile.Address.City,
            profile.Address.District,
            profile.Address.Postcode,
            profile.Address.Country);

        var verificationDto = new VerificationStateDto(
            profile.Verification.Outcome.ToString(),
            profile.Verification.Method.ToString(),
            profile.Verification.EvidenceRef,
            profile.Verification.RejectionReason,
            profile.Verification.LastAttemptUtc);

        var images = profile.Images.Select(img => new CompanyImageDto(img.Id, img.File, img.UploadedOnUtc)).ToList();
        var documents = profile.Documents.Select(doc => new SupplementaryDocumentDto(doc.Id, doc.File, doc.Kind.ToString(), doc.UploadedOnUtc)).ToList();

        var dto = new EmployerProfileDto(
            profile.Id,
            profile.UserId,
            profile.Status.ToString(),
            profile.CompanyName.Value,
            profile.Email.Value,
            profile.Mobile.Value,
            profile.CompanyIdentifier.Value,
            profile.Website?.Value,
            profile.Industry,
            profile.CompanySize?.Value.ToString(),
            addressDto,
            profile.Description?.Value,
            profile.Logo,
            images,
            documents,
            verificationDto,
            profile.Completeness.Level1Complete,
            profile.Completeness.Level2Complete);

        return Result.Success(dto);
    }
}
