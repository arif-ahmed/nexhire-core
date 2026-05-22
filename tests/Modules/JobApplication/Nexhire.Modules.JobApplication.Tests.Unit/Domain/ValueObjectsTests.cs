using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.Domain;

public class ValueObjectsTests
{
    [Fact]
    public void CoverLetter_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CoverLetter.Create("This is my cover letter.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("This is my cover letter.");
    }

    [Fact]
    public void CoverLetter_Create_Should_Fail_WhenTextIsEmpty()
    {
        // Act
        var result = CoverLetter.Create("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CoverLetter.Empty");
    }

    [Fact]
    public void CoverLetter_Create_Should_Fail_WhenTextIsTooLong()
    {
        // Arrange
        var longText = new string('A', 4001);

        // Act
        var result = CoverLetter.Create(longText);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CoverLetter.TooLong");
    }

    [Fact]
    public void WithdrawalReason_Create_Should_Succeed_WhenCodeIsValid()
    {
        // Act
        var result = WithdrawalReason.Create("ChangedMind", "I changed my mind");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("ChangedMind");
        result.Value.Comment.Should().Be("I changed my mind");
    }

    [Fact]
    public void WithdrawalReason_Create_Should_Fail_WhenCodeIsInvalid()
    {
        // Act
        var result = WithdrawalReason.Create("InvalidCode", "Comment");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WithdrawalReason.InvalidCode");
    }

    [Fact]
    public void WithdrawalReason_Create_Should_Fail_WhenCommentIsTooLong()
    {
        // Arrange
        var longComment = new string('C', 1001);

        // Act
        var result = WithdrawalReason.Create("ChangedMind", longComment);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WithdrawalReason.CommentTooLong");
    }

    [Fact]
    public void CandidateSnapshot_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CandidateSnapshot.Create(
            "John Doe",
            "john@example.com",
            "1234567890",
            "New York",
            "B.Sc. in CS",
            "5 years exp",
            new List<string> { "C#", "DDD" },
            isLevel2Complete: true,
            DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Email.Should().Be("john@example.com");
        result.Value.IsLevel2Complete.Should().BeTrue();
    }

    [Fact]
    public void CandidateSnapshot_Create_Should_Fail_WhenProfileLevel2IsCompleteIsFalse()
    {
        // Act
        var result = CandidateSnapshot.Create(
            "John Doe",
            "john@example.com",
            "1234567890",
            "New York",
            "B.Sc. in CS",
            "5 years exp",
            new List<string> { "C#" },
            isLevel2Complete: false,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateSnapshot.ProfileIncomplete");
    }

    [Fact]
    public void CandidateSnapshot_Create_Should_Fail_WhenEmailIsEmpty()
    {
        // Act
        var result = CandidateSnapshot.Create(
            "John Doe",
            "",
            "1234567890",
            "New York",
            "B.Sc. in CS",
            "5 years exp",
            new List<string> { "C#" },
            isLevel2Complete: true,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateSnapshot.EmptyEmail");
    }
}
