using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

public interface IJobSeekerProfileRepository
{
    Task<Aggregates.JobSeekerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Aggregates.JobSeekerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Aggregates.JobSeekerProfile?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> IsSlugTakenAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Aggregates.JobSeekerProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(Aggregates.JobSeekerProfile profile, CancellationToken cancellationToken = default);
}
