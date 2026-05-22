using System;
using Nexhire.Modules.JobApplication.Core.Domain.Events;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobApplication.Core.Domain;

public class Bookmark : AggregateRoot<BookmarkId>
{
    public Guid JobSeekerId { get; private set; }
    public Guid JobPostingId { get; private set; }
    public DateTime BookmarkedOnUtc { get; private set; }

    private Bookmark(BookmarkId id, Guid jobSeekerId, Guid jobPostingId, DateTime bookmarkedOnUtc) : base(id)
    {
        JobSeekerId = jobSeekerId;
        JobPostingId = jobPostingId;
        BookmarkedOnUtc = bookmarkedOnUtc;
    }

    private Bookmark()
    {
        // Required by EF Core
    }

    public static Bookmark Create(Guid jobSeekerId, Guid jobPostingId)
    {
        var bookmarkId = BookmarkId.New();
        var bookmarkedOnUtc = DateTime.UtcNow;
        var bookmark = new Bookmark(bookmarkId, jobSeekerId, jobPostingId, bookmarkedOnUtc);

        bookmark.RaiseDomainEvent(new JobBookmarkedIntegrationEvent(
            Guid.NewGuid(),
            jobSeekerId,
            jobPostingId,
            bookmarkedOnUtc));

        return bookmark;
    }

    public void Unbookmark()
    {
        RaiseDomainEvent(new JobUnbookmarkedIntegrationEvent(
            Guid.NewGuid(),
            JobSeekerId,
            JobPostingId,
            DateTime.UtcNow));
    }
}
