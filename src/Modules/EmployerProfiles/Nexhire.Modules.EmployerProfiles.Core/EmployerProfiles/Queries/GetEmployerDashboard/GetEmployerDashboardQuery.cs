using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerDashboard;

public record GetEmployerDashboardQuery(Guid UserId) : IQuery<EmployerDashboardDto>;

public class GetEmployerDashboardQueryHandler : IQueryHandler<GetEmployerDashboardQuery, EmployerDashboardDto>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IDashboardProjectionStore _projectionStore;

    public GetEmployerDashboardQueryHandler(
        IEmployerProfileRepository employerRepository,
        IDashboardProjectionStore projectionStore)
    {
        _employerRepository = employerRepository;
        _projectionStore = projectionStore;
    }

    public async Task<Result<EmployerDashboardDto>> Handle(GetEmployerDashboardQuery request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<EmployerDashboardDto>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var postings = await _projectionStore.GetPostingsAsync(request.UserId, cancellationToken);
        var applications = await _projectionStore.GetApplicationsAsync(request.UserId, cancellationToken);
        var matches = await _projectionStore.GetMatchedCandidatesAsync(request.UserId, cancellationToken);

        var activePostingsCount = postings.Count(p => p.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("Published", StringComparison.OrdinalIgnoreCase));
        
        var dto = new EmployerDashboardDto(
            activePostingsCount,
            applications.Count,
            matches.Count);

        return Result.Success(dto);
    }
}
