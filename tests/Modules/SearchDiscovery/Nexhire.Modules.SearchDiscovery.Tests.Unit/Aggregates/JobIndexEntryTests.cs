using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Aggregates;

public class JobIndexEntryTests
{
    private static readonly Guid PostingId = Guid.NewGuid();
    private static readonly Guid EmployerId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    private static Result<JobIndexEntry> CreateValidEntry(long sourceVersion = 1)
    {
        return JobIndexEntry.Project(
            PostingId, EmployerId, "Senior Developer", "Build amazing things",
            "Acme Corp", ["C#", "SQL"], "BSc", 5,
            "Dhaka", null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            50000, 80000, "BDT", "IT",
            Now, Now.AddMonths(1), sourceVersion, Now);
    }

    [Fact]
    public void Project_ShouldCreateEntry_WhenValidPayload()
    {
        var result = CreateValidEntry();

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(PostingId);
        result.Value.EmployerId.Should().Be(EmployerId);
        result.Value.Title.Should().Be("Senior Developer");
        result.Value.Skills.Should().BeEquivalentTo(["C#", "SQL"]);
        result.Value.SourcePostingVersion.Should().Be(1);
    }

    [Fact]
    public void Project_ShouldRaiseJobIndexedEvent()
    {
        var entry = CreateValidEntry().Value;

        entry.DomainEvents.Should().ContainSingle(e => e is JobIndexed);
    }

    [Fact]
    public void Project_ShouldSetSourcePostingVersion()
    {
        var entry = CreateValidEntry(sourceVersion: 42).Value;

        entry.SourcePostingVersion.Should().Be(42);
    }

    [Fact]
    public void ApplyUpdate_ShouldRefreshFields_WhenNewerVersion()
    {
        var entry = CreateValidEntry(sourceVersion: 1).Value;
        entry.ClearDomainEvents();

        var result = entry.ApplyUpdate("Lead Developer", "Updated summary", ["C#", "Azure"],
            "MSc", 7, null, null, null, null,
            EmploymentType.FullTime, WorkFormat.Hybrid,
            60000, 90000, "BDT", "Tech",
            Now.AddMonths(2), 2);

        result.IsSuccess.Should().BeTrue();
        entry.Title.Should().Be("Lead Developer");
        entry.SourcePostingVersion.Should().Be(2);
    }

    [Fact]
    public void ApplyUpdate_ShouldBeNoOp_WhenStaleVersion()
    {
        var entry = CreateValidEntry(sourceVersion: 5).Value;
        entry.ClearDomainEvents();

        var result = entry.ApplyUpdate("Old Title", null, null,
            null, null, null, null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            null, null, null, null,
            null, 3);

        result.IsSuccess.Should().BeTrue();
        entry.Title.Should().Be("Senior Developer");
        entry.SourcePostingVersion.Should().Be(5);
    }

    [Fact]
    public void ApplyUpdate_ShouldBeNoOp_WhenEqualVersion()
    {
        var entry = CreateValidEntry(sourceVersion: 5).Value;
        entry.ClearDomainEvents();

        var result = entry.ApplyUpdate("Repeated Title", null, null,
            null, null, null, null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            null, null, null, null,
            null, 5);

        result.IsSuccess.Should().BeTrue();
        entry.Title.Should().Be("Senior Developer");
    }

    [Fact]
    public void Project_ShouldSetLocation_WhenProvided()
    {
        var result = JobIndexEntry.Project(
            PostingId, EmployerId, "Dev", "Summary",
            "Corp", [], null, null,
            "Dhaka", "Gulshan", 23.7, 90.4,
            EmploymentType.FullTime, WorkFormat.OnSite,
            null, null, null, null,
            Now, null, 1, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Location.District.Should().Be("Dhaka");
        result.Value.Location.City.Should().Be("Gulshan");
    }
}
