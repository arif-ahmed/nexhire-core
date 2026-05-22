using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.CreateSavedSearch;
using Nexhire.Shared.Core.Results;
using NSubstitute;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Application;

public class CreateSavedSearchTests
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateSavedSearchCommandHandler _handler;

    public CreateSavedSearchTests()
    {
        _savedSearchRepo = Substitute.For<ISavedSearchRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateSavedSearchCommandHandler(_savedSearchRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenValidInput()
    {
        var seekerId = Guid.NewGuid();
        _savedSearchRepo.IsNameTakenAsync(seekerId, "My Search", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new CreateSavedSearchCommand(
            seekerId, "My Search", "developer", null, NotificationPreference.Instant), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _savedSearchRepo.Received(1).AddAsync(Arg.Any<SavedSearch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenDuplicateNameForSameSeeker()
    {
        var seekerId = Guid.NewGuid();
        _savedSearchRepo.IsNameTakenAsync(seekerId, "My Search", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new CreateSavedSearchCommand(
            seekerId, "My Search", "developer", null, NotificationPreference.Instant), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-SAVED-SEARCH-NAME-DUPLICATE");
    }

    [Fact]
    public async Task Handle_ShouldAllow_SameNameForDifferentSeeker()
    {
        var seeker1 = Guid.NewGuid();
        var seeker2 = Guid.NewGuid();
        _savedSearchRepo.IsNameTakenAsync(seeker2, "My Search", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new CreateSavedSearchCommand(
            seeker2, "My Search", "developer", null, NotificationPreference.Instant), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
