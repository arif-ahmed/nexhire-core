using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Aggregates;

public class SavedSearchTests
{
    private static readonly Guid SeekerId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    private static SearchCriteria ValidCriteria => SearchCriteria.Create(
        keyword: "developer",
        allowEmptyForPersistence: false).Value;

    [Fact]
    public void Create_ShouldSucceed_WhenValidInput()
    {
        var result = SavedSearch.Create(SeekerId, "My Search", ValidCriteria,
            NotificationPreference.Instant, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeekerUserId.Should().Be(SeekerId);
        result.Value.Name.Should().Be("My Search");
        result.Value.NotificationPreference.Should().Be(NotificationPreference.Instant);
    }

    [Fact]
    public void Create_ShouldRaiseSavedSearchCreatedEvent()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.DailyDigest, Now).Value;

        saved.DomainEvents.Should().ContainSingle(e => e is SavedSearchCreated);
    }

    [Fact]
    public void Create_ShouldFail_WhenNameEmpty()
    {
        var result = SavedSearch.Create(SeekerId, "", ValidCriteria,
            NotificationPreference.None, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SavedSearch.EmptyName");
    }

    [Fact]
    public void Create_ShouldFail_WhenNameOver100Chars()
    {
        var result = SavedSearch.Create(SeekerId, new string('a', 101), ValidCriteria,
            NotificationPreference.None, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SavedSearch.NameTooLong");
    }

    [Fact]
    public void Create_ShouldFail_WhenSeekerIdEmpty()
    {
        var result = SavedSearch.Create(Guid.Empty, "Test", ValidCriteria,
            NotificationPreference.None, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SavedSearch.EmptySeekerUserId");
    }

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var saved = SavedSearch.Create(SeekerId, "Old", ValidCriteria,
            NotificationPreference.None, Now).Value;
        saved.ClearDomainEvents();

        var result = saved.Rename("New Name");

        result.IsSuccess.Should().BeTrue();
        saved.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_ShouldFail_WhenNameEmpty()
    {
        var saved = SavedSearch.Create(SeekerId, "Old", ValidCriteria,
            NotificationPreference.None, Now).Value;

        var result = saved.Rename("");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateCriteria_ShouldUpdateCriteria()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.None, Now).Value;
        saved.ClearDomainEvents();

        var newCriteria = SearchCriteria.Create(keyword: "architect").Value;
        var result = saved.UpdateCriteria(newCriteria);

        result.IsSuccess.Should().BeTrue();
        saved.Criteria.Keyword.Should().Be("architect");
    }

    [Fact]
    public void SetNotificationPreference_ShouldUpdatePreference()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.None, Now).Value;
        saved.ClearDomainEvents();

        var result = saved.SetNotificationPreference(NotificationPreference.WeeklyDigest);

        result.IsSuccess.Should().BeTrue();
        saved.NotificationPreference.Should().Be(NotificationPreference.WeeklyDigest);
    }

    [Fact]
    public void SetNotificationPreference_NoneShouldKeepRow()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.Instant, Now).Value;

        var result = saved.SetNotificationPreference(NotificationPreference.None);

        result.IsSuccess.Should().BeTrue();
        saved.NotificationPreference.Should().Be(NotificationPreference.None);
        saved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldMarkDeleted()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.None, Now).Value;

        var result = saved.Delete();

        result.IsSuccess.Should().BeTrue();
        saved.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void RecordEvaluated_ShouldStampLastEvaluatedOnUtc()
    {
        var saved = SavedSearch.Create(SeekerId, "Test", ValidCriteria,
            NotificationPreference.None, Now).Value;
        var evaluatedAt = Now.AddHours(1);

        saved.RecordEvaluated(evaluatedAt);

        saved.LastEvaluatedOnUtc.Should().Be(evaluatedAt);
    }
}
