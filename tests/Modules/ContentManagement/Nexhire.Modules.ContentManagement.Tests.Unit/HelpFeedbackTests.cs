using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class HelpFeedbackTests
{
    [Fact]
    public void Submit_Helpful_Succeeds()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), true, null, "Great!", "JobSeeker", Language.En);
        result.IsSuccess.Should().BeTrue();
        result.Value.WasHelpful.Should().BeTrue();
        result.Value.Reason.Should().BeNull();
    }

    [Fact]
    public void Submit_NotHelpful_WithReason_Succeeds()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), false, FeedbackReason.Unclear, "Needs more detail", null, Language.En);
        result.IsSuccess.Should().BeTrue();
        result.Value.WasHelpful.Should().BeFalse();
        result.Value.Reason.Should().Be(FeedbackReason.Unclear);
        result.Value.SubmittedByRole.Should().BeNull();
    }

    [Fact]
    public void Submit_NotHelpful_WithoutReason_Fails()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), false, null, null, null, Language.En);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FEEDBACK-REASON-REQUIRED");
    }

    [Fact]
    public void Submit_Helpful_WithReason_Fails()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), true, FeedbackReason.Other, null, null, Language.En);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FEEDBACK-REASON-FORBIDDEN");
    }

    [Fact]
    public void Submit_CommentTooLong_Fails()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), true, null, new string('x', 2001), null, Language.En);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FEEDBACK-COMMENT-TOO-LONG");
    }

    [Fact]
    public void Submit_Anonymous_Succeeds()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), true, null, null, null, Language.Bn);
        result.IsSuccess.Should().BeTrue();
        result.Value.SubmittedByRole.Should().BeNull();
        result.Value.Language.Should().Be(Language.Bn);
    }

    [Fact]
    public void Submit_RaisesHelpFeedbackReceivedEvent()
    {
        var result = HelpFeedback.Submit(Guid.NewGuid(), false, FeedbackReason.Incomplete, null, null, Language.En);
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().NotBeEmpty();
    }
}
