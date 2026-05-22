using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;

public class SavedSearch : AggregateRoot<Guid>
{
    public Guid SeekerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SearchCriteria Criteria { get; private set; } = null!;
    public NotificationPreference NotificationPreference { get; private set; }
    public DateTime? LastEvaluatedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public bool IsDeleted { get; private set; }

    private SavedSearch() { }

    public static Result<SavedSearch> Create(
        Guid seekerUserId,
        string name,
        SearchCriteria criteria,
        NotificationPreference notificationPreference,
        DateTime nowUtc)
    {
        if (seekerUserId == Guid.Empty)
            return Result.Failure<SavedSearch>(new Error("SavedSearch.EmptySeekerUserId", "Seeker user ID is required."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<SavedSearch>(new Error("SavedSearch.EmptyName", "Name is required."));

        if (name.Length > 100)
            return Result.Failure<SavedSearch>(new Error("SavedSearch.NameTooLong", "Name cannot exceed 100 characters."));

        var saved = new SavedSearch
        {
            Id = Guid.NewGuid(),
            SeekerUserId = seekerUserId,
            Name = name.Trim(),
            Criteria = criteria,
            NotificationPreference = notificationPreference,
            CreatedOnUtc = nowUtc,
            UpdatedOnUtc = nowUtc
        };

        saved.RaiseDomainEvent(new SavedSearchCreated(Guid.NewGuid(), saved.Id, seekerUserId, name, nowUtc));
        return Result.Success(saved);
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(new Error("SavedSearch.EmptyName", "Name is required."));

        if (newName.Length > 100)
            return Result.Failure(new Error("SavedSearch.NameTooLong", "Name cannot exceed 100 characters."));

        Name = newName.Trim();
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateCriteria(SearchCriteria newCriteria)
    {
        Criteria = newCriteria;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result SetNotificationPreference(NotificationPreference preference)
    {
        NotificationPreference = preference;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new SavedSearchNotificationChanged(
            Guid.NewGuid(), Id, preference.ToString(), DateTime.UtcNow));
        return Result.Success();
    }

    public Result Delete()
    {
        IsDeleted = true;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void RecordEvaluated(DateTime nowUtc)
    {
        LastEvaluatedOnUtc = nowUtc;
    }
}
