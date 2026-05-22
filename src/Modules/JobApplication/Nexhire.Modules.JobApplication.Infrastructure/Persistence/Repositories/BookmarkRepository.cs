using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;

public sealed class BookmarkRepository : IBookmarkRepository
{
    private readonly JobApplicationDbContext _dbContext;

    public BookmarkRepository(JobApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Bookmark?> GetByIdAsync(BookmarkId id, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookmarks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Bookmark?> GetAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookmarks
            .FirstOrDefaultAsync(x => x.JobSeekerId == seekerId && x.JobPostingId == postingId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Bookmark>> ListBySeekerAsync(Guid seekerId, CancellationToken cancellationToken)
    {
        var list = await _dbContext.Bookmarks
            .Where(x => x.JobSeekerId == seekerId)
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task AddAsync(Bookmark bookmark, CancellationToken cancellationToken)
    {
        await _dbContext.Bookmarks.AddAsync(bookmark, cancellationToken);
    }

    public void Remove(Bookmark bookmark)
    {
        _dbContext.Bookmarks.Remove(bookmark);
    }
}
