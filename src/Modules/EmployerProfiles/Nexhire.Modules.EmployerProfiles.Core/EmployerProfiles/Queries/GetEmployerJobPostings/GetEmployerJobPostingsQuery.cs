using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerJobPostings;

public record GetEmployerJobPostingsQuery(Guid UserId) : IQuery<List<DashboardPostingDto>>;
