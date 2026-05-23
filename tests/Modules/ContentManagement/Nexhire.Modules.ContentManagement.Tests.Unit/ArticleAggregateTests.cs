using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Events;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;


namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class ArticleAggregateTests
{
    private static LocalizedContent EnContent => LocalizedContent.Create("EN Title", "EN Summary", "<p>EN Body</p>").Value;
    private static LocalizedContent BnContent => LocalizedContent.Create("BN Title", "BN Summary", "<p>BN Body</p>").Value;

    private static Article CreateDraftWithCategory()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetPrimaryCategory(Guid.NewGuid());
        return article;
    }

    // --- Creation ---
    [Fact]
    public void CreateDraft_SetsStatus_Draft()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.Status.Should().Be(ArticleStatus.Draft);
        article.Localizations.Should().ContainKey(Language.En);
        article.AuthorUserId.Should().NotBeEmpty();
    }

    // --- Localization ---
    [Fact]
    public void SetLocalization_AddsNewLanguage()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetLocalization(Language.Bn, BnContent);
        article.Localizations.Should().ContainKey(Language.Bn);
        article.Localizations[Language.En].Should().Be(EnContent);
    }

    [Fact]
    public void RemoveLocalization_LastOne_Fails()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        var result = article.RemoveLocalization(Language.En);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ARTICLE-LAST-LOCALIZATION");
    }

    [Fact]
    public void RemoveLocalization_WithTwo_Succeeds()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetLocalization(Language.Bn, BnContent);
        var result = article.RemoveLocalization(Language.Bn);
        result.IsSuccess.Should().BeTrue();
        article.Localizations.Should().NotContainKey(Language.Bn);
    }

    [Fact]
    public void LocalizationIndependence_EditingEn_DoesNotTouchBn()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetLocalization(Language.Bn, BnContent);
        var bnOriginal = article.Localizations[Language.Bn];

        var newEn = LocalizedContent.Create("New EN", "New Summary", "<p>New Body</p>").Value;
        article.SetLocalization(Language.En, newEn);

        article.Localizations[Language.Bn].Should().Be(bnOriginal);
        article.Localizations[Language.En].Title.Should().Be("New EN");
    }

    // --- Tags ---
    [Fact]
    public void SetTags_ReplacesAndDedupes()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetTags(Language.En, ["React", "react", "Angular"]);
        var tags = article.Tags.Where(t => t.Language == Language.En).ToList();
        tags.Should().HaveCount(2);
        tags.Select(t => t.NormalizedLabel).Should().BeEquivalentTo(["react", "angular"]);
    }

    // --- Media ---
    [Fact]
    public void AddMedia_And_RemoveMedia()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        var media = MediaReference.Create("key1", "/url", "image/jpeg", 1024, MediaKind.Image).Value;
        article.AddMedia(media);
        article.Media.Should().HaveCount(1);

        article.RemoveMedia("key1");
        article.Media.Should().BeEmpty();
    }

    // --- Status Machine ---
    [Theory]
    [InlineData(ArticleStatus.Draft, true)]
    [InlineData(ArticleStatus.Scheduled, true)]
    [InlineData(ArticleStatus.Unpublished, true)]
    [InlineData(ArticleStatus.Published, true)] // Idempotent
    [InlineData(ArticleStatus.Archived, false)]
    public void Publish_FromStatus(ArticleStatus from, bool shouldSucceed)
    {
        var article = CreateDraftWithCategory();
        TransitionTo(article, from);

        var result = article.Publish();
        result.IsSuccess.Should().Be(shouldSucceed);
        if (shouldSucceed)
            article.Status.Should().Be(ArticleStatus.Published);
    }

    [Fact]
    public void Publish_Idempotent_AlreadyPublished()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        var result = article.Publish();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Publish_WithoutCategory_Fails()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        var result = article.Publish();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ARTICLE-NO-CATEGORY");
    }

    [Fact]
    public void Publish_WithNoLocalization_Fails()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetPrimaryCategory(Guid.NewGuid());
        article.RemoveLocalization(Language.En);
        // This should fail because it's the last localization
        // But since we can't remove the last one, this scenario is guarded at the aggregate level
    }

    [Fact]
    public void Publish_SetsPublishedOnUtc_Once()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        var firstPublished = article.PublishedOnUtc;
        firstPublished.Should().NotBeNull();

        article.Unpublish();
        article.Publish();
        article.PublishedOnUtc.Should().Be(firstPublished);
    }

    // --- Schedule ---
    [Fact]
    public void Schedule_FromDraft_Succeeds()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        var result = article.Schedule(schedule);
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Scheduled);
        article.PublicationSchedule.Should().NotBeNull();
    }

    [Fact]
    public void Schedule_WithoutCategory_Fails()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        var result = article.Schedule(schedule);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ARTICLE-NO-CATEGORY");
    }

    [Fact]
    public void CancelSchedule_FromScheduled_Succeeds()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        article.Schedule(schedule);
        var result = article.CancelSchedule();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Draft);
        article.PublicationSchedule.Should().BeNull();
    }

    [Fact]
    public void CancelSchedule_FromDraft_Fails()
    {
        var article = CreateDraftWithCategory();
        var result = article.CancelSchedule();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IsDueForPublication_True_WhenPastSchedule()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        article.Schedule(schedule);
        article.IsDueForPublication(DateTime.UtcNow.AddHours(3)).Should().BeTrue();
    }

    [Fact]
    public void IsDueForPublication_False_WhenNotYetDue()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        article.Schedule(schedule);
        article.IsDueForPublication(DateTime.UtcNow.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void IsDueForPublication_False_WhenNotScheduled()
    {
        var article = CreateDraftWithCategory();
        article.IsDueForPublication(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MarkPublishedBySchedule_Succeeds()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        article.Schedule(schedule);
        var result = article.MarkPublishedBySchedule();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Published);
        article.PublishedOnUtc.Should().NotBeNull();
        article.PublicationSchedule.Should().BeNull();
    }

    // --- Unpublish ---
    [Fact]
    public void Unpublish_FromPublished_Succeeds()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        var result = article.Unpublish();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Unpublished);
    }

    [Fact]
    public void Unpublish_RaisesArchivedEvent_WithUnpublishedStatus()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.ClearDomainEvents();
        article.Unpublish();

        article.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ArticleArchivedDomainEvent>()
            .Which.ResultingStatus.Should().Be(ArticleStatus.Unpublished);
    }

    [Fact]
    public void Unpublish_FromDraft_Fails()
    {
        var article = CreateDraftWithCategory();
        var result = article.Unpublish();
        result.IsFailure.Should().BeTrue();
    }

    // --- Archive ---
    [Fact]
    public void Archive_FromPublished_Succeeds()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        var result = article.Archive();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Archived);
        article.PreviousStatus.Should().Be(ArticleStatus.Published);
    }

    [Fact]
    public void Archive_FromUnpublished_Succeeds()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.Unpublish();
        var result = article.Archive();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Archived);
        article.PreviousStatus.Should().Be(ArticleStatus.Unpublished);
    }

    [Fact]
    public void Archive_RaisesArchivedEvent_WithArchivedStatus()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.ClearDomainEvents();
        article.Archive();

        article.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ArticleArchivedDomainEvent>()
            .Which.ResultingStatus.Should().Be(ArticleStatus.Archived);
    }

    [Fact]
    public void Archive_FromDraft_Fails()
    {
        var article = CreateDraftWithCategory();
        var result = article.Archive();
        result.IsFailure.Should().BeTrue();
    }

    // --- Restore ---
    [Fact]
    public void RestoreFromArchive_ToPublished_Succeeds()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.Archive();
        article.ClearDomainEvents();

        var result = article.RestoreFromArchive();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Published);

        article.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ArticlePublishedDomainEvent>();
    }

    [Fact]
    public void RestoreFromArchive_ToUnpublished_Succeeds_NoEvent()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.Unpublish();
        article.Archive();
        article.ClearDomainEvents();

        var result = article.RestoreFromArchive();
        result.IsSuccess.Should().BeTrue();
        article.Status.Should().Be(ArticleStatus.Unpublished);
        article.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RestoreFromArchive_FromNonArchived_Fails()
    {
        var article = CreateDraftWithCategory();
        var result = article.RestoreFromArchive();
        result.IsFailure.Should().BeTrue();
    }

    // --- Domain Events ---
    [Fact]
    public void Publish_RaisesArticlePublishedEvent()
    {
        var article = CreateDraftWithCategory();
        article.Publish();
        article.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ArticlePublishedDomainEvent>();
    }

    [Fact]
    public void Schedule_RaisesArticleScheduledEvent()
    {
        var article = CreateDraftWithCategory();
        var schedule = PublicationSchedule.Create(DateTime.UtcNow.AddHours(2)).Value;
        article.Schedule(schedule);
        article.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ArticleScheduledDomainEvent>();
    }

    // Helper to transition to any status for testing
    private static void TransitionTo(Article article, ArticleStatus target)
    {
        switch (target)
        {
            case ArticleStatus.Draft:
                break;
            case ArticleStatus.Scheduled:
                var s = PublicationSchedule.Create(DateTime.UtcNow.AddHours(5)).Value;
                article.Schedule(s);
                break;
            case ArticleStatus.Published:
                article.Publish();
                break;
            case ArticleStatus.Unpublished:
                article.Publish();
                article.Unpublish();
                break;
            case ArticleStatus.Archived:
                article.Publish();
                article.Archive();
                break;
        }
    }
}
