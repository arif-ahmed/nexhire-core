namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

public interface IAdminActionLogRepository
{
    Task AddAsync(AdminActionLog log, CancellationToken ct = default);
    Task<object> QueryAsync(object criteria, CancellationToken ct = default); // Stubbing PagedResult and AdminActionLogQuery
}
