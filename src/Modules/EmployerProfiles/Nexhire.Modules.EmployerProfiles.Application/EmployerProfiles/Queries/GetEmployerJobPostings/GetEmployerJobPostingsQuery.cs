using Nexhire.Modules.EmployerProfiles.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Queries.GetEmployerJobPostings;

public record GetEmployerJobPostingsQuery(Guid UserId) : IQuery<List<DashboardPostingDto>>;
