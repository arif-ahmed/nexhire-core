using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

public interface IDashboardProjectionStore
{
    Task UpsertPostingAsync(DashboardPosting posting, CancellationToken cancellationToken = default);
    Task RemovePostingAsync(Guid postingId, CancellationToken cancellationToken = default);
    Task AddApplicationAsync(DashboardApplication application, CancellationToken cancellationToken = default);
    Task UpsertMatchedCandidateAsync(DashboardMatchedCandidate candidate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardPosting>> GetPostingsAsync(Guid employerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardApplication>> GetApplicationsAsync(Guid employerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardMatchedCandidate>> GetMatchedCandidatesAsync(Guid employerUserId, CancellationToken cancellationToken = default);
}
