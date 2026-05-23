using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class ValueObjectTests
{
    // LocalizedContent
    [Fact]
    public void LocalizedContent_Create_Valid_Succeeds()
    {
        var result = LocalizedContent.Create("Title", "Summary", "<p>Body</p>");
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Title");
        result.Value.Summary.Should().Be("Summary");
    }

    [Fact]
    public void LocalizedContent_Create_EmptyTitle_Fails()
    {
        var result = LocalizedContent.Create("", "Summary", "<p>Body</p>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CONTENT-TITLE-EMPTY");
    }

    [Fact]
    public void LocalizedContent_Create_TitleTooLong_Fails()
    {
        var result = LocalizedContent.Create(new string('x', 201), "Summary", "<p>Body</p>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CONTENT-TITLE-TOO-LONG");
    }

    [Fact]
    public void LocalizedContent_Create_SummaryTooLong_Fails()
    {
        var result = LocalizedContent.Create("Title", new string('x', 501), "<p>Body</p>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CONTENT-SUMMARY-TOO-LONG");
    }

    [Fact]
    public void LocalizedContent_Create_EmptyBody_Fails()
    {
        var result = LocalizedContent.Create("Title", "Summary", "");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CONTENT-BODY-EMPTY");
    }

    [Fact]
    public void LocalizedContent_Create_ScriptTagStripped()
    {
        var result = LocalizedContent.Create("Title", "Summary", "<script>alert('x')</script><p>Body</p>");
        result.IsSuccess.Should().BeTrue();
        result.Value.BodyRichText.Should().NotContain("<script>");
    }

    // FaqContent
    [Fact]
    public void FaqContent_Create_Valid_Succeeds()
    {
        var result = FaqContent.Create("What is X?", "<p>Answer</p>");
        result.IsSuccess.Should().BeTrue();
        result.Value.Question.Should().Be("What is X?");
    }

    [Fact]
    public void FaqContent_Create_EmptyQuestion_Fails()
    {
        var result = FaqContent.Create("", "<p>Answer</p>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FAQ-QUESTION-EMPTY");
    }

    [Fact]
    public void FaqContent_Create_QuestionTooLong_Fails()
    {
        var result = FaqContent.Create(new string('x', 301), "<p>Answer</p>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FAQ-QUESTION-TOO-LONG");
    }

    [Fact]
    public void FaqContent_Create_EmptyAnswer_Fails()
    {
        var result = FaqContent.Create("Question?", "");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FAQ-ANSWER-EMPTY");
    }

    // ArticleTag
    [Fact]
    public void ArticleTag_Create_Valid_Succeeds()
    {
        var result = ArticleTag.Create(Language.En, "React");
        result.IsSuccess.Should().BeTrue();
        result.Value.NormalizedLabel.Should().Be("react");
        result.Value.DisplayLabel.Should().Be("React");
    }

    [Fact]
    public void ArticleTag_Create_Empty_Fails()
    {
        var result = ArticleTag.Create(Language.En, "");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAG-EMPTY");
    }

    [Fact]
    public void ArticleTag_Create_TooLong_Fails()
    {
        var result = ArticleTag.Create(Language.En, new string('x', 51));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAG-TOO-LONG");
    }

    // MediaReference
    [Fact]
    public void MediaReference_Create_ValidImage_Succeeds()
    {
        var result = MediaReference.Create("key1", "/url", "image/jpeg", 1024, MediaKind.Image);
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(MediaKind.Image);
    }

    [Fact]
    public void MediaReference_Create_ValidVideo_Succeeds()
    {
        var result = MediaReference.Create("key2", "/url", "video/mp4", 1024 * 1024, MediaKind.Video);
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(MediaKind.Video);
    }

    [Fact]
    public void MediaReference_Create_ImageOver5MB_Fails()
    {
        var result = MediaReference.Create("key", "/url", "image/jpeg", 6 * 1024 * 1024, MediaKind.Image);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-SIZE-EXCEEDED");
    }

    [Fact]
    public void MediaReference_Create_VideoOver500MB_Fails()
    {
        var result = MediaReference.Create("key", "/url", "video/mp4", 600L * 1024 * 1024, MediaKind.Video);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-SIZE-EXCEEDED");
    }

    [Fact]
    public void MediaReference_Create_WrongImageMime_Fails()
    {
        var result = MediaReference.Create("key", "/url", "image/bmp", 1024, MediaKind.Image);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-INVALID-FORMAT");
    }

    [Fact]
    public void MediaReference_Create_WrongVideoMime_Fails()
    {
        var result = MediaReference.Create("key", "/url", "video/avi", 1024, MediaKind.Video);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-INVALID-FORMAT");
    }

    [Fact]
    public void MediaReference_Create_EmptyKey_Fails()
    {
        var result = MediaReference.Create("", "/url", "image/jpeg", 1024, MediaKind.Image);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-KEY-EMPTY");
    }

    [Fact]
    public void MediaReference_Create_ZeroSize_Fails()
    {
        var result = MediaReference.Create("key", "/url", "image/jpeg", 0, MediaKind.Image);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MEDIA-SIZE-INVALID");
    }

    // PublicationSchedule
    [Fact]
    public void PublicationSchedule_Create_Future_Succeeds()
    {
        var result = PublicationSchedule.Create(DateTime.UtcNow.AddHours(1));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PublicationSchedule_Create_Past_Fails()
    {
        var result = PublicationSchedule.Create(DateTime.UtcNow.AddHours(-1));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-SCHEDULE-PAST");
    }

    // VisibleRoleSet
    [Fact]
    public void VisibleRoleSet_Create_Valid_Succeeds()
    {
        var result = VisibleRoleSet.Create([VisibleRole.JobSeeker, VisibleRole.Employer]);
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().Contain(VisibleRole.JobSeeker);
    }

    [Fact]
    public void VisibleRoleSet_Create_Empty_Fails()
    {
        var result = VisibleRoleSet.Create([]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ROLES-EMPTY");
    }

    [Fact]
    public void VisibleRoleSet_Create_AllMixed_Fails()
    {
        var result = VisibleRoleSet.Create([VisibleRole.All, VisibleRole.JobSeeker]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ROLES-ALL-MIXED");
    }

    [Fact]
    public void VisibleRoleSet_ContainsAll_ReturnsTrueForAny()
    {
        var result = VisibleRoleSet.Create([VisibleRole.All]);
        result.IsSuccess.Should().BeTrue();
        result.Value.Contains(VisibleRole.JobSeeker).Should().BeTrue();
        result.Value.Contains(VisibleRole.Employer).Should().BeTrue();
    }

    // AudienceSet
    [Fact]
    public void AudienceSet_Create_Valid_Succeeds()
    {
        var result = AudienceSet.Create([Audience.JobSeekers]);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AudienceSet_Create_Empty_Fails()
    {
        var result = AudienceSet.Create([]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-AUDIENCE-EMPTY");
    }

    // MultimediaBlock
    [Fact]
    public void MultimediaBlock_CreateVideo_Valid_Succeeds()
    {
        var media = MediaReference.Create("key", "/url", "video/mp4", 1024, MediaKind.Video).Value;
        var result = MultimediaBlock.CreateVideo(media);
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockKind.Should().Be(MediaBlockKind.Video);
    }

    [Fact]
    public void MultimediaBlock_CreateVideo_WithImageMedia_Fails()
    {
        var media = MediaReference.Create("key", "/url", "image/jpeg", 1024, MediaKind.Image).Value;
        var result = MultimediaBlock.CreateVideo(media);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MULTIMEDIA-KIND-MISMATCH");
    }

    [Fact]
    public void MultimediaBlock_CreateStepGuide_Valid_Succeeds()
    {
        var step = GuideStep.Create(1, "Step 1").Value;
        var result = MultimediaBlock.CreateStepGuide([step]);
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockKind.Should().Be(MediaBlockKind.StepGuide);
    }

    [Fact]
    public void MultimediaBlock_CreateStepGuide_NoSteps_Fails()
    {
        var result = MultimediaBlock.CreateStepGuide([]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-MULTIMEDIA-NO-STEPS");
    }

    // GuideStep
    [Fact]
    public void GuideStep_Create_EmptyCaption_Fails()
    {
        var result = GuideStep.Create(1, "");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-GUIDE-STEP-CAPTION-EMPTY");
    }

    [Fact]
    public void GuideStep_Create_VideoAsImage_Fails()
    {
        var videoMedia = MediaReference.Create("key", "/url", "video/mp4", 1024, MediaKind.Video).Value;
        var result = GuideStep.Create(1, "Caption", videoMedia);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-GUIDE-STEP-IMAGE-KIND");
    }

    // TourAction
    [Fact]
    public void TourAction_Create_NavigateNoPayload_Fails()
    {
        var result = TourAction.Create(TourActionKind.Navigate);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOUR-ACTION-NO-PAYLOAD");
    }

    [Fact]
    public void TourAction_Create_NavigateWithPayload_Succeeds()
    {
        var result = TourAction.Create(TourActionKind.Navigate, "/jobs");
        result.IsSuccess.Should().BeTrue();
        result.Value.Payload.Should().Be("/jobs");
    }
}
