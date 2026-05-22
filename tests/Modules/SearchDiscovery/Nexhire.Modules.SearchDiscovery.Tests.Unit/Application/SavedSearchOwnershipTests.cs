using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RenameSavedSearch;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DeleteSavedSearch;
using Nexhire.Shared.Core.Results;
using NSubstitute;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Application;

public class SavedSearchOwnershipTests
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SavedSearchOwnershipTests()
    {
        _savedSearchRepo = Substitute.For<ISavedSearchRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    [Fact]
    public async Task RenameSavedSearch_ShouldFail_WhenNonOwner()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var saved = SavedSearch.Create(owner, "Test",
            SearchCriteria.Create(keyword: "dev").Value, NotificationPreference.None, DateTime.UtcNow).Value;
        _savedSearchRepo.GetByIdAsync(saved.Id, Arg.Any<CancellationToken>()).Returns(saved);
        _savedSearchRepo.IsNameTakenAsync(other, "New", saved.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RenameSavedSearchCommandHandler(_savedSearchRepo, _unitOfWork);

        var result = await handler.Handle(new RenameSavedSearchCommand(saved.Id, other, "New"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FORBIDDEN");
    }

    [Fact]
    public async Task DeleteSavedSearch_ShouldFail_WhenNonOwner()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var saved = SavedSearch.Create(owner, "Test",
            SearchCriteria.Create(keyword: "dev").Value, NotificationPreference.None, DateTime.UtcNow).Value;
        _savedSearchRepo.GetByIdAsync(saved.Id, Arg.Any<CancellationToken>()).Returns(saved);
        var handler = new DeleteSavedSearchCommandHandler(_savedSearchRepo, _unitOfWork);

        var result = await handler.Handle(new DeleteSavedSearchCommand(saved.Id, other), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FORBIDDEN");
    }
}
