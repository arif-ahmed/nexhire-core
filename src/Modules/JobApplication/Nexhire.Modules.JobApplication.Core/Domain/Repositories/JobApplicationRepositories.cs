using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexhire.Modules.JobApplication.Core.Domain.Repositories;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(ApplicationId id, CancellationToken cancellationToken);
    Task<Application?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken);
    Task<Application?> GetNonTerminalForAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> GetTerminalForAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> GetNonTerminalByPostingAsync(Guid postingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> GetNonTerminalBySeekerAsync(Guid seekerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> ListBySeekerAsync(Guid seekerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> ListByPostingAsync(Guid postingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Application>> ListByPostingsAsync(IEnumerable<Guid> postingIds, CancellationToken cancellationToken);
    Task AddAsync(Application application, CancellationToken cancellationToken);
    void Update(Application application);
}

public interface IBookmarkRepository
{
    Task<Bookmark?> GetByIdAsync(BookmarkId id, CancellationToken cancellationToken);
    Task<Bookmark?> GetAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Bookmark>> ListBySeekerAsync(Guid seekerId, CancellationToken cancellationToken);
    Task AddAsync(Bookmark bookmark, CancellationToken cancellationToken);
    void Remove(Bookmark bookmark);
}

public interface IIdempotencyKeyStore
{
    Task<Guid?> TryGetAsync(Guid idempotencyKey, CancellationToken cancellationToken);
    Task SaveAsync(Guid idempotencyKey, ApplicationId applicationId, CancellationToken cancellationToken);
    Task PurgeOlderThanAsync(DateTime threshold, CancellationToken cancellationToken);
}

public interface IJobApplicationUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
