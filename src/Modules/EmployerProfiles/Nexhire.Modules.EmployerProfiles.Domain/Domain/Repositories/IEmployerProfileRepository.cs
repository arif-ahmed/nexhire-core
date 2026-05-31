using Nexhire.Modules.EmployerProfiles.Domain.Aggregates;

namespace Nexhire.Modules.EmployerProfiles.Domain.Repositories;

public interface IEmployerProfileRepository
{
    Task<EmployerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CompanyIdentifierExistsAsync(string companyIdentifier, CancellationToken cancellationToken = default);
    Task AddAsync(EmployerProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmployerProfile profile, CancellationToken cancellationToken = default);
}
