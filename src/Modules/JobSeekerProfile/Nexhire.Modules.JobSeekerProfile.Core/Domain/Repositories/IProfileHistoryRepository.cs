using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

public interface IProfileHistoryRepository
{
    Task<ProfileHistory?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task AddAsync(ProfileHistory history, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProfileHistory history, CancellationToken cancellationToken = default);
}
