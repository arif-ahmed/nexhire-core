using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CompleteEmployerLevel2;

public class CompleteEmployerLevel2CommandHandler : ICommandHandler<CompleteEmployerLevel2Command>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteEmployerLevel2CommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CompleteEmployerLevel2Command request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var websiteResult = WebsiteUrl.Create(request.Website);
        if (websiteResult.IsFailure) return Result.Failure(websiteResult.Error);

        var companySizeResult = CompanySize.Create(request.CompanySize);
        if (companySizeResult.IsFailure) return Result.Failure(companySizeResult.Error);

        var addressResult = Address.Create(
            request.Address.Line1,
            request.Address.Line2,
            request.Address.City,
            request.Address.District,
            request.Address.Postcode,
            request.Address.Country);
        if (addressResult.IsFailure) return Result.Failure(addressResult.Error);

        var descResult = CompanyDescription.Create(request.Description);
        if (descResult.IsFailure) return Result.Failure(descResult.Error);

        var completeResult = profile.CompleteLevel2(
            websiteResult.Value,
            request.Industry,
            companySizeResult.Value,
            addressResult.Value,
            descResult.Value);

        if (completeResult.IsFailure)
        {
            return completeResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
