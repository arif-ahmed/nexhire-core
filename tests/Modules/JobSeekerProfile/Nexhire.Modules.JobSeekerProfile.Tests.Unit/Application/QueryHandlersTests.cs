using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetEditHistory;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetMyProfile;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetProfileCompleteness;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetPublicProfile;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetResumeParseStatus;
using Xunit;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit.Application;

public class QueryHandlersTests
{
    private readonly IJobSeekerProfileRepository _profileRepository = Substitute.For<IJobSeekerProfileRepository>();
    private readonly IProfileHistoryRepository _historyRepository = Substitute.For<IProfileHistoryRepository>();
    private readonly IResumeRepository _resumeRepository = Substitute.For<IResumeRepository>();

    private static PersonName GetValidName() => PersonName.Create("John", "Doe").Value;
    private static EmailAddress GetValidEmail() => EmailAddress.Create("john.doe@nexhire.com").Value;
    private static MobileNumber GetValidMobile() => MobileNumber.Create("+8801712345678").Value;

    private Aggregates.JobSeekerProfile CreateActiveProfile(Guid userId)
    {
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), userId, GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        profile.Activate();
        return profile;
    }

    private Aggregates.JobSeekerProfile CreateCompleteProfile(Guid userId)
    {
        var profile = CreateActiveProfile(userId);
        var period = DateRange.Create(DateTime.UtcNow.AddYears(-1), null).Value;
        profile.AddEducation("B.Sc.", "Uni", period, 3.5m);
        profile.AddExperience("A", "Dev", period, true, "Responsibilities");
        
        var address = Address.Create("123 St", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        profile.SetAddresses(address, null);

        var prefs = JobPreferences.Create(new[] { "Full-time" }, new[] { "Tech" }, new[] { "Dhaka" }, new[] { WorkArrangement.Remote }, null).Value;
        profile.SetPreferences(prefs);

        var skillRef = CanonicalSkillRef.Create("SK-1", "C#").Value;
        profile.AddSkill(skillRef, "C#", SkillCategory.Hard, SkillTier.Primary, 5);

        var file = FileReference.Create("k", "d.pdf", "application/pdf", 1024).Value;
        profile.AddSupplementaryDocument(file, DocumentKind.Certificate, VirusScanResult.Create(VirusScanStatus.Clean).Value);

        profile.MarkResumeAttached();
        return profile;
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnProfile_WhenProfileExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var query = new GetMyProfileQuery(userId);
        var handler = new GetMyProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be(profile.Email.Value);
    }

    [Fact]
    public async Task GetMyProfile_ShouldFail_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Aggregates.JobSeekerProfile)null!);

        var query = new GetMyProfileQuery(userId);
        var handler = new GetMyProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task GetProfileCompleteness_ShouldReturnCompleteness_WhenProfileExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var query = new GetProfileCompletenessQuery(userId);
        var handler = new GetProfileCompletenessQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Percentage.Should().Be(30);
    }

    [Fact]
    public async Task GetProfileCompleteness_ShouldFail_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Aggregates.JobSeekerProfile)null!);

        var query = new GetProfileCompletenessQuery(userId);
        var handler = new GetProfileCompletenessQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task GetResumeParseStatus_ShouldReturnStatus_WhenActiveResumeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var fileRef = FileReference.Create("key", "cv.pdf", "application/pdf", 1024).Value;
        var resume = Aggregates.Resume.Upload(Guid.NewGuid(), profile.Id, fileRef).Value;
        _resumeRepository.GetActiveByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(resume);

        var query = new GetResumeParseStatusQuery(userId);
        var handler = new GetResumeParseStatusQueryHandler(_profileRepository, _resumeRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParseStatus.Should().Be(ResumeParseStatus.Uploaded.ToString());
    }

    [Fact]
    public async Task GetResumeParseStatus_ShouldFail_WhenNoResumeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        _resumeRepository.GetActiveByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns((Aggregates.Resume)null!);

        var query = new GetResumeParseStatusQuery(userId);
        var handler = new GetResumeParseStatusQueryHandler(_profileRepository, _resumeRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Resume.NotFound");
    }

    [Fact]
    public async Task GetEditHistory_ShouldReturnHistory_WhenHistoryExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _profileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var history = Aggregates.ProfileHistory.Start(Guid.NewGuid(), profile.Id).Value;
        _historyRepository.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(history);

        var query = new GetEditHistoryQuery(userId);
        var handler = new GetEditHistoryQueryHandler(_profileRepository, _historyRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.JobSeekerProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task GetPublicProfile_ShouldFail_WhenProfileNotFoundBySlug()
    {
        // Arrange
        var slug = "john-doe-1234";
        _profileRepository.GetBySlugAsync(slug, Arg.Any<CancellationToken>()).Returns((Aggregates.JobSeekerProfile)null!);

        var query = new GetPublicProfileQuery(slug);
        var handler = new GetPublicProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task GetPublicProfile_ShouldFail_WhenProfileIsNotActive()
    {
        // Arrange
        var slug = "john-doe-1234";
        var userId = Guid.NewGuid();
        
        // This profile remains in PendingActivation status.
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), userId, GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        
        _profileRepository.GetBySlugAsync(slug, Arg.Any<CancellationToken>()).Returns(profile);

        var query = new GetPublicProfileQuery(slug);
        var handler = new GetPublicProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task GetPublicProfile_ShouldFail_WhenPublicSharingIsDisabled()
    {
        // Arrange
        var slug = "john-doe-1234";
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId); // Active but public sharing disabled

        _profileRepository.GetBySlugAsync(slug, Arg.Any<CancellationToken>()).Returns(profile);

        var query = new GetPublicProfileQuery(slug);
        var handler = new GetPublicProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobSeekerProfile.NotFound");
    }

    [Fact]
    public async Task GetPublicProfile_ShouldSucceed_WhenProfileIsActiveAndPublicSharingIsEnabled()
    {
        // Arrange
        var slug = "john-doe-1234";
        var userId = Guid.NewGuid();
        var profile = CreateCompleteProfile(userId); // Active and 100% complete
        
        var qrCodeFile = FileReference.Create("qr-key", "qr.png", "image/png", 512).Value;
        profile.EnablePublicSharing(slug, qrCodeFile); // Successfully enable public sharing

        _profileRepository.GetBySlugAsync(slug, Arg.Any<CancellationToken>()).Returns(profile);

        var query = new GetPublicProfileQuery(slug);
        var handler = new GetPublicProfileQueryHandler(_profileRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.PublicSharing.Enabled.Should().BeTrue();
        result.Value.PublicSharing.Slug.Should().Be(slug);
    }
}
