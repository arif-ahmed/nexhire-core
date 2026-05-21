using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetPublicEmployerProfile;

public record GetPublicEmployerProfileQuery(Guid EmployerProfileId) : IQuery<PublicEmployerProfileDto>;

public class GetPublicEmployerProfileQueryHandler : IQueryHandler<GetPublicEmployerProfileQuery, PublicEmployerProfileDto>
{
    private readonly IEmployerProfileRepository _repository;

    public GetPublicEmployerProfileQueryHandler(IEmployerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PublicEmployerProfileDto>> Handle(GetPublicEmployerProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.EmployerProfileId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<PublicEmployerProfileDto>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var addressDto = profile.Address == null ? null : new AddressDto(
            profile.Address.Line1,
            profile.Address.Line2,
            profile.Address.City,
            profile.Address.District,
            profile.Address.Postcode,
            profile.Address.Country);

        var dto = new PublicEmployerProfileDto(
            profile.Id,
            profile.CompanyName.Value,
            profile.Website?.Value,
            profile.Industry,
            profile.CompanySize?.Value.ToString(),
            addressDto,
            profile.Description?.Value,
            profile.Logo);

        return Result.Success(dto);
    }
}
