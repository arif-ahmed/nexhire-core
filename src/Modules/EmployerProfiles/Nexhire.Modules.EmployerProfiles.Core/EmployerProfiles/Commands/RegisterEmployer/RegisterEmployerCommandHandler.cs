using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RegisterEmployer;

public class RegisterEmployerCommandHandler : ICommandHandler<RegisterEmployerCommand, Guid>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IIdentityProvisioningApi _identityApi;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterEmployerCommandHandler(
        IEmployerProfileRepository repository,
        IIdentityProvisioningApi identityApi,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _identityApi = identityApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterEmployerCommand request, CancellationToken cancellationToken)
    {
        var exists = await _repository.CompanyIdentifierExistsAsync(request.CompanyIdentifier, cancellationToken);
        if (exists)
        {
            return Result.Failure<Guid>(new Error("E-REG-DUPLICATE", "Company registration number already exists."));
        }

        var provisionResult = await _identityApi.ProvisionCredentialAsync(
            request.Email,
            request.Mobile,
            request.Password,
            "Employer",
            cancellationToken);

        if (provisionResult.IsFailure)
        {
            return Result.Failure<Guid>(provisionResult.Error);
        }

        var companyName = CompanyName.Create(request.CompanyName).Value;
        var email = EmailAddress.Create(request.Email).Value;
        var mobile = MobileNumber.Create(request.Mobile).Value;
        var companyIdentifier = CompanyIdentifier.Create(request.CompanyIdentifier).Value;

        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            provisionResult.Value.UserId,
            companyName,
            email,
            mobile,
            companyIdentifier);

        await _repository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profile.Id);
    }
}
