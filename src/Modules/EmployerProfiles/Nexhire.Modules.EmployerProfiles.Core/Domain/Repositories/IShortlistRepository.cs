using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

public interface IShortlistRepository
{
    Task<Shortlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shortlist>> GetByEmployerProfileIdAsync(Guid employerProfileId, CancellationToken cancellationToken = default);
    Task AddAsync(Shortlist shortlist, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shortlist shortlist, CancellationToken cancellationToken = default);
}
