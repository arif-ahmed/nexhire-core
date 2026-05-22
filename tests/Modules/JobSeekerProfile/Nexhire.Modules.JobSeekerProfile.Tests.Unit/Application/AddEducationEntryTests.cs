using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddEducationEntry;
using Nexhire.Shared.Core.Results;
using Xunit;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit.Application;

public class AddEducationEntryTests
{
    private readonly IJobSeekerProfileRepository _repository = Substitute.For<IJobSeekerProfileRepository>();
    private readonly IProfileHistoryRepository _historyRepository = Substitute.For<IProfileHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AddEducationEntryCommandHandler _handler;

    public AddEducationEntryTests()
    {
        _handler = new AddEducationEntryCommandHandler(_repository, _historyRepository, _unitOfWork);
    }

    private Aggregates.JobSeekerProfile CreateProfile(Guid userId)
    {
        var name = PersonName.Create("John", "Doe").Value;
        var email = EmailAddress.Create("john.doe@example.com").Value;
        var mobile = MobileNumber.Create("+8801712345678").Value;
        return Aggregates.JobSeekerProfile.Register(
            Guid.NewGuid(),
            userId,
            name,
            email,
            mobile,
            Gender.Male).Value;
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new AddEducationEntryCommand(
            userId,
            "Bachelor of Science",
            "University of Dhaka",
            DateTime.UtcNow.AddYears(-4),
            DateTime.UtcNow.AddYears(-1),
            3.8m);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Aggregates.JobSeekerProfile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenHistoryNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var command = new AddEducationEntryCommand(
            userId,
            "Bachelor of Science",
            "University of Dhaka",
            DateTime.UtcNow.AddYears(-4),
            DateTime.UtcNow.AddYears(-1),
            3.8m);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _historyRepository.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((ProfileHistory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProfileHistory.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndAddEducationAndAppendHistory_WhenInputsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var history = ProfileHistory.Start(Guid.NewGuid(), profile.Id).Value;

        var command = new AddEducationEntryCommand(
            userId,
            "Bachelor of Science",
            "University of Dhaka",
            DateTime.UtcNow.AddYears(-4),
            DateTime.UtcNow.AddYears(-1),
            3.8m);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _historyRepository.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(history);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Education.Should().HaveCount(1);
        profile.Education.First().Degree.Should().Be(command.Degree);
        profile.Education.First().Institution.Should().Be(command.Institution);
        profile.Education.First().Gpa.Should().Be(command.Gpa);

        history.Versions.Should().HaveCount(1);
        history.Versions.First().ChangedFields.Should().Contain("Education");

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _historyRepository.Received(1).UpdateAsync(history, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new AddEducationEntryCommandValidator();
        var command = new AddEducationEntryCommand(
            Guid.Empty,
            "",
            "",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            3.8m);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEducationEntryCommand.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEducationEntryCommand.Degree));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEducationEntryCommand.Institution));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEducationEntryCommand.EndDate));
    }
}
