using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Services;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.Domain;

public class DomainServicesTests
{
    [Fact]
    public void CandidateSnapshotBuilder_Build_Should_ApplyOverrides_WhenProvided()
    {
        // Arrange
        var profile = new JobSeekerProfileSnapshotDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@example.com",
            "1234567890",
            "New York",
            "B.Sc. in CS",
            "5 years exp",
            new List<string> { "C#" },
            IsLevel2Complete: true,
            "Public");

        var overrides = new SnapshotOverrides(
            FullName: "Johnny Doe",
            Email: "johnny@example.com",
            Mobile: "0987654321");

        // Act
        var result = CandidateSnapshotBuilder.Build(profile, overrides);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var snapshot = result.Value;
        snapshot.FullName.Should().Be("Johnny Doe");
        snapshot.Email.Should().Be("johnny@example.com");
        snapshot.Mobile.Should().Be("0987654321");
        snapshot.CurrentLocation.Should().Be("New York"); // Unaffected by overrides
        snapshot.EducationSummary.Should().Be("B.Sc. in CS");
        snapshot.IsLevel2Complete.Should().BeTrue();
    }

    [Fact]
    public void ApplicationEligibilityService_CheckCanApply_Should_Succeed_WhenPostingIsActiveAndProfileIsEligible()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", DateTime.UtcNow.AddDays(5));

        // Act
        var result = ApplicationEligibilityService.CheckCanApply(
            seekerId,
            postingId,
            posting,
            isLevel2Complete: true,
            existingNonTerminalApplication: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ApplicationEligibilityService_CheckCanApply_Should_Fail_WhenNonTerminalApplicationExists()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", DateTime.UtcNow.AddDays(5));
        
        var snapshot = CandidateSnapshot.Create(
            "Name", "email@test.com", "123", "Loc", "Edu", "Exp", new List<string> { "Skill" },
            isLevel2Complete: true, DateTime.UtcNow).Value;
            
        var existingApp = Application.Submit(
            postingId, seekerId, Guid.NewGuid(), snapshot, Guid.NewGuid(), null, null, Guid.NewGuid()).Value;

        // Act
        var result = ApplicationEligibilityService.CheckCanApply(
            seekerId,
            postingId,
            posting,
            isLevel2Complete: true,
            existingNonTerminalApplication: existingApp);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-DUPLICATE");
    }

    [Fact]
    public void ApplicationEligibilityService_CheckCanApply_Should_Fail_WhenPostingIsClosed()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Closed", DateTime.UtcNow.AddDays(5));

        // Act
        var result = ApplicationEligibilityService.CheckCanApply(
            seekerId,
            postingId,
            posting,
            isLevel2Complete: true,
            existingNonTerminalApplication: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-POSTING-CLOSED");
    }

    [Fact]
    public void ApplicationEligibilityService_CheckCanApply_Should_Fail_WhenDeadlinePassed()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = ApplicationEligibilityService.CheckCanApply(
            seekerId,
            postingId,
            posting,
            isLevel2Complete: true,
            existingNonTerminalApplication: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-POSTING-CLOSED");
    }

    [Fact]
    public void ApplicationEligibilityService_CheckCanApply_Should_Fail_WhenLevel2IsIncomplete()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", DateTime.UtcNow.AddDays(5));

        // Act
        var result = ApplicationEligibilityService.CheckCanApply(
            seekerId,
            postingId,
            posting,
            isLevel2Complete: false,
            existingNonTerminalApplication: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-PROFILE-INCOMPLETE");
    }
}
