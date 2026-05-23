using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class FaqEntryTests
{
    private static FaqContent EnContent => FaqContent.Create("What is X?", "<p>Answer</p>").Value;

    [Fact]
    public void CreateDraft_SetsStatus_Draft()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        entry.Status.Should().Be(ContentStatus.Draft);
        entry.Kind.Should().Be(FaqEntryKind.Faq);
    }

    [Fact]
    public void Publish_Succeeds()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var result = entry.Publish();
        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ContentStatus.Published);
    }

    [Fact]
    public void Publish_Idempotent()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        entry.Publish();
        var result = entry.Publish();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_Succeeds()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        entry.Publish();
        var result = entry.Unpublish();
        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public void Unpublish_FromDraft_Fails()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var result = entry.Unpublish();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddMultimediaBlock_OnFaqKind_Fails()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var media = MediaReference.Create("key", "/url", "video/mp4", 1024, MediaKind.Video).Value;
        var block = MultimediaBlock.CreateVideo(media).Value;
        var result = entry.AddMultimediaBlock(block);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FAQ-NOT-HELP-ARTICLE");
    }

    [Fact]
    public void AddMultimediaBlock_OnHelpArticle_Succeeds()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.HelpArticle, Language.En, EnContent);
        var media = MediaReference.Create("key", "/url", "video/mp4", 1024, MediaKind.Video).Value;
        var block = MultimediaBlock.CreateVideo(media).Value;
        var result = entry.AddMultimediaBlock(block);
        result.IsSuccess.Should().BeTrue();
        entry.MultimediaBlocks.Should().HaveCount(1);
    }

    [Fact]
    public void SetTopics_Replaces()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        entry.SetTopics([t1, t2, t1]); // Deduped
        entry.TopicIds.Should().BeEquivalentTo([t1, t2]);
    }

    [Fact]
    public void SetVisibleRoles_Succeeds()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var roles = VisibleRoleSet.Create([VisibleRole.JobSeeker]).Value;
        entry.SetVisibleRoles(roles);
        entry.VisibleRoles.Should().NotBeNull();
    }

    [Fact]
    public void SetContextKeys_Replaces()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        entry.SetContextKeys(["job-posting.create", "profile.edit", "job-posting.create"]);
        entry.ContextKeys.Should().BeEquivalentTo(["job-posting.create", "profile.edit"]);
    }

    [Fact]
    public void OverwriteOnEdit_NoVersionHistory()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var newContent = FaqContent.Create("Updated Q?", "<p>Updated A</p>").Value;
        entry.SetLocalization(Language.En, newContent);
        entry.Localizations[Language.En].Question.Should().Be("Updated Q?");
    }

    [Fact]
    public void RemoveLocalization_LastOne_Fails()
    {
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, EnContent);
        var result = entry.RemoveLocalization(Language.En);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FAQ-LAST-LOCALIZATION");
    }
}
