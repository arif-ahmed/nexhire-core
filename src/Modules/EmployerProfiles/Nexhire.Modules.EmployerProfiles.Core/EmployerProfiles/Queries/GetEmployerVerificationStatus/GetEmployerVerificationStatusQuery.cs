using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerVerificationStatus;

public record GetEmployerVerificationStatusQuery(Guid UserId) : IQuery<VerificationStateDto>;

public class GetEmployerVerificationStatusQueryHandler : IQueryHandler<GetEmployerVerificationStatusQuery, VerificationStateDto>
{
    private readonly IEmployerProfileRepository _repository;

    public GetEmployerVerificationStatusQueryHandler(IEmployerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<VerificationStateDto>> Handle(GetEmployerVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<VerificationStateDto>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var verificationDto = new VerificationStateDto(
            profile.Verification.Outcome.ToString(),
            profile.Verification.Method.ToString(),
            profile.Verification.EvidenceRef,
            profile.Verification.RejectionReason,
            profile.Verification.LastAttemptUtc);

        return Result.Success(verificationDto);
    }
}
