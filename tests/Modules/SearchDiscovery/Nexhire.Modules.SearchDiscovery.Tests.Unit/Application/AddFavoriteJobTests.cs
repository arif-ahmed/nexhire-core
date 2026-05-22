using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.AddFavoriteJob;
using Nexhire.Shared.Core.Results;
using NSubstitute;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Application;

public class AddFavoriteJobTests
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IFavoriteJobRepository _favoriteRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddFavoriteJobCommandHandler _handler;

    public AddFavoriteJobTests()
    {
        _jobIndexRepo = Substitute.For<IJobIndexEntryRepository>();
        _favoriteRepo = Substitute.For<IFavoriteJobRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AddFavoriteJobCommandHandler(_jobIndexRepo, _favoriteRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenPostingExists()
    {
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        _jobIndexRepo.ExistsAsync(postingId, Arg.Any<CancellationToken>()).Returns(true);
        _favoriteRepo.GetBySeekerAndPostingAsync(seekerId, postingId, Arg.Any<CancellationToken>()).Returns((FavoriteJob?)null);

        var result = await _handler.Handle(new AddFavoriteJobCommand(seekerId, postingId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _favoriteRepo.Received(1).AddAsync(Arg.Any<FavoriteJob>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPostingNotInIndex()
    {
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        _jobIndexRepo.ExistsAsync(postingId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new AddFavoriteJobCommand(seekerId, postingId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-JOB-NOT-FOUND");
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAlreadyFavorited()
    {
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var existing = FavoriteJob.Add(seekerId, postingId, DateTime.UtcNow).Value;
        _jobIndexRepo.ExistsAsync(postingId, Arg.Any<CancellationToken>()).Returns(true);
        _favoriteRepo.GetBySeekerAndPostingAsync(seekerId, postingId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new AddFavoriteJobCommand(seekerId, postingId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        await _favoriteRepo.DidNotReceive().AddAsync(Arg.Any<FavoriteJob>(), Arg.Any<CancellationToken>());
    }
}
