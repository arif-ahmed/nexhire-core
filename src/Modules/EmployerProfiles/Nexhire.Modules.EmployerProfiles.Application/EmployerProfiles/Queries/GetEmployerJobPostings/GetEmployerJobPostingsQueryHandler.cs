using Nexhire.Modules.EmployerProfiles.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Application.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Queries.GetEmployerJobPostings;

public class GetEmployerJobPostingsQueryHandler : IQueryHandler<GetEmployerJobPostingsQuery, List<DashboardPostingDto>>
{
    private readonly IDashboardProjectionStore _dashboardStore;

    public GetEmployerJobPostingsQueryHandler(IDashboardProjectionStore dashboardStore)
    {
        _dashboardStore = dashboardStore;
    }

    public async Task<Result<List<DashboardPostingDto>>> Handle(GetEmployerJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var postings = await _dashboardStore.GetPostingsAsync(request.UserId, cancellationToken);

        var dtos = postings
            .Select(p => new DashboardPostingDto(p.PostingId, p.Title, p.Status, p.LastEventOnUtc))
            .ToList();

        return Result.Success(dtos);
    }
}
