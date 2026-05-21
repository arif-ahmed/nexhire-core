using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CreateShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RenameShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.DeleteShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.AddCandidateToShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCandidateFromShortlist;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class ShortlistTests
{
    private readonly IEmployerProfileRepository _employerRepository = Substitute.For<IEmployerProfileRepository>();
    private readonly IShortlistRepository _shortlistRepository = Substitute.For<IShortlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private (Guid UserId, EmployerProfile Profile) CreateActiveProfile()
    {
        var userId = Guid.NewGuid();
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create("REG123456").Value);
        profile.Activate();
        return (userId, profile);
    }

    [Fact]
    public async Task CreateShortlist_ShouldSucceed_WhenNameIsValid()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new CreateShortlistCommandHandler(_employerRepository, _shortlistRepository, _unitOfWork);
        var command = new CreateShortlistCommand(userId, "Senior Developers");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _shortlistRepository.Received(1).AddAsync(Arg.Is<Shortlist>(s => 
            s.EmployerProfileId == profile.Id && 
            s.Name == "Senior Developers"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameShortlist_ShouldSucceed_WhenValid()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlist = Shortlist.Create(Guid.NewGuid(), profile.Id, "Old Name").Value;
        _shortlistRepository.GetByIdAsync(shortlist.Id, Arg.Any<CancellationToken>())
            .Returns(shortlist);

        var handler = new RenameShortlistCommandHandler(_employerRepository, _shortlistRepository, _unitOfWork);
        var command = new RenameShortlistCommand(userId, shortlist.Id, "New Name");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shortlist.Name.Should().Be("New Name");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteShortlist_ShouldSucceed()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlist = Shortlist.Create(Guid.NewGuid(), profile.Id, "To Delete").Value;
        _shortlistRepository.GetByIdAsync(shortlist.Id, Arg.Any<CancellationToken>())
            .Returns(shortlist);

        var handler = new DeleteShortlistCommandHandler(_employerRepository, _shortlistRepository, _unitOfWork);
        var command = new DeleteShortlistCommand(userId, shortlist.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shortlist.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddCandidateToShortlist_ShouldSucceed()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlist = Shortlist.Create(Guid.NewGuid(), profile.Id, "Java Devs").Value;
        _shortlistRepository.GetByIdAsync(shortlist.Id, Arg.Any<CancellationToken>())
            .Returns(shortlist);

        var candidateId = Guid.NewGuid();
        var handler = new AddCandidateToShortlistCommandHandler(_employerRepository, _shortlistRepository, _unitOfWork);
        var command = new AddCandidateToShortlistCommand(userId, shortlist.Id, candidateId, 85);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shortlist.Members.Should().HaveCount(1);
        shortlist.Members.First().CandidateUserId.Should().Be(candidateId);
        shortlist.Members.First().MatchScore.Should().Be(85);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveCandidateFromShortlist_ShouldSucceed()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlist = Shortlist.Create(Guid.NewGuid(), profile.Id, "Java Devs").Value;
        var candidateId = Guid.NewGuid();
        shortlist.AddCandidate(candidateId, 90);
        var memberId = shortlist.Members.First().Id;

        _shortlistRepository.GetByIdAsync(shortlist.Id, Arg.Any<CancellationToken>())
            .Returns(shortlist);

        var handler = new RemoveCandidateFromShortlistCommandHandler(_employerRepository, _shortlistRepository, _unitOfWork);
        var command = new RemoveCandidateFromShortlistCommand(userId, shortlist.Id, memberId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shortlist.Members.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
