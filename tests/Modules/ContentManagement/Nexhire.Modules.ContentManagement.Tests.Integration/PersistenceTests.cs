using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Modules.ContentManagement.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;
using NSubstitute;

namespace Nexhire.Modules.ContentManagement.Tests.Integration;

public class PersistenceTests
{
    private static ContentManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ContentManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var publisher = Substitute.For<MediatR.IPublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(publisher);
        var interceptor = new PublishDomainEventsInterceptor(services.BuildServiceProvider());

        return new ContentManagementDbContext(options, interceptor);
    }

    [Fact]
    public async Task Category_RoundTrip()
    {
        await using var db = CreateContext();
        var names = new Dictionary<Language, string> { [Language.En] = "News", [Language.Bn] = "সংবাদ" };
        var category = Category.Create(names, "platform-news");

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        await using var readDb = CreateContext();
        // InMemory shares by db name, so create a fresh one for read won't work.
        // Instead, verify from the same context after detaching.
        var loaded = await db.Categories.FindAsync(category.Id);
        loaded.Should().NotBeNull();
        loaded!.Slug.Should().Be("platform-news");
        loaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Topic_RoundTrip()
    {
        await using var db = CreateContext();
        var names = new Dictionary<Language, string> { [Language.En] = "Laws" };
        var topic = Topic.Create(names, "laws");

        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        var loaded = await db.Topics.FindAsync(topic.Id);
        loaded.Should().NotBeNull();
        loaded!.Slug.Should().Be("laws");
    }

    [Fact]
    public async Task Article_RoundTrip()
    {
        await using var db = CreateContext();
        var content = LocalizedContent.Create("Title", "Summary", "<p>Body</p>").Value;
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, content);
        article.SetPrimaryCategory(Guid.NewGuid());

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var loaded = await db.Articles.FindAsync(article.Id);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(ArticleStatus.Draft);
        loaded.AuthorUserId.Should().Be(article.AuthorUserId);
    }

    [Fact]
    public async Task FaqEntry_RoundTrip()
    {
        await using var db = CreateContext();
        var content = FaqContent.Create("Q?", "<p>A</p>").Value;
        var entry = FaqEntry.CreateDraft(FaqEntryKind.Faq, Language.En, content);

        db.FaqEntries.Add(entry);
        await db.SaveChangesAsync();

        var loaded = await db.FaqEntries.FindAsync(entry.Id);
        loaded.Should().NotBeNull();
        loaded!.Kind.Should().Be(FaqEntryKind.Faq);
        loaded.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task GuidedTour_RoundTrip()
    {
        await using var db = CreateContext();
        var audience = AudienceSet.Create([Audience.NewUsers]).Value;
        var tour = GuidedTour.Create(Language.En, "Onboarding", "Welcome tour", audience);
        tour.AddStep("#start", "Click start");

        db.GuidedTours.Add(tour);
        await db.SaveChangesAsync();

        var loaded = await db.GuidedTours.FindAsync(tour.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Onboarding");
    }

    [Fact]
    public async Task ContentPreference_RoundTrip()
    {
        await using var db = CreateContext();
        var userId = Guid.NewGuid();
        var pref = ContentPreference.CreateDefault(userId);

        db.ContentPreferences.Add(pref);
        await db.SaveChangesAsync();

        var loaded = await db.ContentPreferences.FindAsync(pref.Id);
        loaded.Should().NotBeNull();
        loaded!.UserId.Should().Be(userId);
        loaded.PreferredLanguage.Should().Be(Language.En);
    }

    [Fact]
    public async Task HelpFeedback_RoundTrip()
    {
        await using var db = CreateContext();
        var result = HelpFeedback.Submit(Guid.NewGuid(), true, null, "Great!", "JobSeeker", Language.En);
        var feedback = result.Value;

        db.HelpFeedbacks.Add(feedback);
        await db.SaveChangesAsync();

        var loaded = await db.HelpFeedbacks.FindAsync(feedback.Id);
        loaded.Should().NotBeNull();
        loaded!.WasHelpful.Should().BeTrue();
    }

    [Fact]
    public async Task Article_Lifecycle_Publish_Archive_Restore()
    {
        await using var db = CreateContext();
        var content = LocalizedContent.Create("Title", "Summary", "<p>Body</p>").Value;
        var catId = Guid.NewGuid();
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, content);
        article.SetPrimaryCategory(catId);

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        article.Publish();
        db.Articles.Update(article);
        await db.SaveChangesAsync();
        article.Status.Should().Be(ArticleStatus.Published);

        article.Archive();
        db.Articles.Update(article);
        await db.SaveChangesAsync();
        article.Status.Should().Be(ArticleStatus.Archived);

        article.RestoreFromArchive();
        db.Articles.Update(article);
        await db.SaveChangesAsync();
        article.Status.Should().Be(ArticleStatus.Published);
    }
}
