using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddSkill;
using Nexhire.Shared.Core.Results;
using Xunit;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit.Application;

public class AddSkillTests
{
    private readonly IJobSeekerProfileRepository _repository = Substitute.For<IJobSeekerProfileRepository>();
    private readonly ITaxonomyApi _taxonomyApi = Substitute.For<ITaxonomyApi>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AddSkillCommandHandler _handler;

    public AddSkillTests()
    {
        _handler = new AddSkillCommandHandler(_repository, _taxonomyApi, _unitOfWork);
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
        var command = new AddSkillCommand(
            userId,
            "C#",
            "Hard",
            "Primary",
            4);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Aggregates.JobSeekerProfile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTaxonomyMappingFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var command = new AddSkillCommand(
            userId,
            "C#",
            "Hard",
            "Primary",
            4);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var taxonomyError = new Error("Taxonomy.NotFound", "Skill not found in taxonomy.");
        _taxonomyApi.MapSkillAsync(command.RawLabel, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CanonicalSkillRef>(taxonomyError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Taxonomy.NotFound");
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndAddSkill_WhenInputsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        var command = new AddSkillCommand(
            userId,
            "C#",
            "Hard",
            "Primary",
            4);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var canonicalRef = CanonicalSkillRef.Create("SK-CS-001", "C# Programming").Value;
        _taxonomyApi.MapSkillAsync(command.RawLabel, Arg.Any<CancellationToken>())
            .Returns(Result.Success(canonicalRef));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Skills.Should().HaveCount(1);
        profile.Skills.First().RawLabel.Should().Be(command.RawLabel);
        profile.Skills.First().CanonicalSkillRef.TaxonomyCode.Should().Be("SK-CS-001");
        profile.Skills.First().Proficiency.Should().Be(4);

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new AddSkillCommandValidator();
        var command = new AddSkillCommand(
            Guid.Empty,
            "",
            "InvalidCategory",
            "InvalidTier",
            6);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSkillCommand.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSkillCommand.RawLabel));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSkillCommand.Category));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSkillCommand.Tier));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSkillCommand.Proficiency));
    }
}
