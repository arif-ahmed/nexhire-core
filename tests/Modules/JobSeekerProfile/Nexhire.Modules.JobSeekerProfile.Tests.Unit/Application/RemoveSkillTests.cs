using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveSkill;
using Nexhire.Shared.Core.Results;
using Xunit;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit.Application;

public class RemoveSkillTests
{
    private readonly IJobSeekerProfileRepository _repository = Substitute.For<IJobSeekerProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RemoveSkillCommandHandler _handler;

    public RemoveSkillTests()
    {
        _handler = new RemoveSkillCommandHandler(_repository, _unitOfWork);
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
        var command = new RemoveSkillCommand(userId, Guid.NewGuid());

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Aggregates.JobSeekerProfile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenSkillNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var command = new RemoveSkillCommand(userId, Guid.NewGuid());

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProfileSkill.NotFound");
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndRemoveSkill_WhenInputsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var canonicalRef = CanonicalSkillRef.Create("SK-CS-001", "C# Programming").Value;
        profile.AddSkill(canonicalRef, "C#", SkillCategory.Hard, SkillTier.Primary, 4);

        var skillId = profile.Skills.First().Id;
        var command = new RemoveSkillCommand(userId, skillId);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Skills.Should().BeEmpty();

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new RemoveSkillCommandValidator();
        var command = new RemoveSkillCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveSkillCommand.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveSkillCommand.SkillId));
    }
}
