using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UpdateEmployerProfile;

public class UpdateEmployerProfileCommandHandler : ICommandHandler<UpdateEmployerProfileCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmployerProfileCommandHandler(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateEmployerProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));

        CompanyName? companyName = null;
        if (request.CompanyName != null)
        {
            var r = CompanyName.Create(request.CompanyName);
            if (r.IsFailure) return Result.Failure(r.Error);
            companyName = r.Value;
        }

        WebsiteUrl? website = null;
        if (request.Website != null)
        {
            var r = WebsiteUrl.Create(request.Website);
            if (r.IsFailure) return Result.Failure(r.Error);
            website = r.Value;
        }

        CompanySize? companySize = null;
        if (request.CompanySize != null)
        {
            var r = CompanySize.Create(request.CompanySize);
            if (r.IsFailure) return Result.Failure(r.Error);
            companySize = r.Value;
        }

        Address? address = null;
        if (request.Address != null)
        {
            var r = Address.Create(
                request.Address.Line1,
                request.Address.Line2,
                request.Address.City,
                request.Address.District,
                request.Address.Postcode,
                request.Address.Country);
            if (r.IsFailure) return Result.Failure(r.Error);
            address = r.Value;
        }

        CompanyDescription? description = null;
        if (request.Description != null)
        {
            var r = CompanyDescription.Create(request.Description);
            if (r.IsFailure) return Result.Failure(r.Error);
            description = r.Value;
        }

        var updateResult = profile.UpdateCompanyInformation(
            companyName,
            website,
            request.Industry,
            companySize,
            address,
            description);

        if (updateResult.IsFailure)
            return updateResult;

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
