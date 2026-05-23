using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Services;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class DomainServiceTests
{
    // --- DashboardNewsSelector ---
    [Fact]
    public void DashboardNews_ManualIncludes_Win()
    {
        var selector = new DashboardNewsSelector();
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();
        var user = Guid.NewGuid();
        var pref = ContentPreference.CreateDefault(user);
        pref.SetIncludedCategories([cat1]);

        var candidates = new List<ArticleSummary>
        {
            new(Guid.NewGuid(), cat1, DateTime.UtcNow, Language.En),
            new(Guid.NewGuid(), cat2, DateTime.UtcNow, Language.En),
        };

        var result = selector.Select(pref, new SeekerPersonalizationAttributes(user, "IT", "Dhaka", ["dev"]), candidates, 10);
        result.Should().ContainSingle();
    }

    [Fact]
    public void DashboardNews_HiddenAlwaysExcluded()
    {
        var selector = new DashboardNewsSelector();
        var cat1 = Guid.NewGuid();
        var user = Guid.NewGuid();
        var pref = ContentPreference.CreateDefault(user);
        pref.SetHiddenCategories([cat1]);

        var candidates = new List<ArticleSummary>
        {
            new(Guid.NewGuid(), cat1, DateTime.UtcNow, Language.En),
            new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, Language.En),
        };

        var result = selector.Select(pref, null, candidates, 10);
        result.Should().ContainSingle();
    }

    [Fact]
    public void DashboardNews_Fallback_WhenNoProfile()
    {
        var selector = new DashboardNewsSelector();
        var user = Guid.NewGuid();
        var pref = ContentPreference.CreateDefault(user);

        var candidates = new List<ArticleSummary>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, Language.En),
            new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), Language.En),
        };

        var result = selector.Select(pref, null, candidates, 10);
        result.Should().HaveCount(2);
    }

    [Fact]
    public void DashboardNews_RespectsMaxItems()
    {
        var selector = new DashboardNewsSelector();
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());

        var candidates = Enumerable.Range(0, 10)
            .Select(i => new ArticleSummary(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-i), Language.En))
            .ToList();

        var result = selector.Select(pref, null, candidates, 3);
        result.Should().HaveCount(3);
    }

    // --- ContentSearchRanker ---
    [Fact]
    public void SearchRanker_TitleMatchesRankedFirst()
    {
        var ranker = new ContentSearchRanker();
        var idBodyOnly = Guid.NewGuid();
        var idTitleMatch = Guid.NewGuid();

        var matches = new List<SearchableContent>
        {
            new(idBodyOnly, "No match", "react framework here", DateTime.UtcNow),
            new(idTitleMatch, "React Basics", "intro text", DateTime.UtcNow),
        };

        var result = ranker.Rank("react", matches);
        result[0].Id.Should().Be(idTitleMatch);
        result[1].Id.Should().Be(idBodyOnly);
    }

    [Fact]
    public void SearchRanker_RecencyTiebreak()
    {
        var ranker = new ContentSearchRanker();
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();

        var matches = new List<SearchableContent>
        {
            new(older, "React Old", "body", DateTime.UtcNow.AddDays(-5)),
            new(newer, "React New", "body", DateTime.UtcNow),
        };

        var result = ranker.Rank("react", matches);
        result[0].Id.Should().Be(newer);
    }

    [Fact]
    public void SearchRanker_NoMatch_EmptyResult()
    {
        var ranker = new ContentSearchRanker();
        var matches = new List<SearchableContent>
        {
            new(Guid.NewGuid(), "Python Guide", "python body", DateTime.UtcNow),
        };

        var result = ranker.Rank("react", matches);
        result.Should().BeEmpty();
    }

    // --- ContextHelpResolver ---
    [Fact]
    public void ContextHelp_FiltersByContextKey()
    {
        var resolver = new ContextHelpResolver();
        var matching = CreatePublishedFaq("job.create", VisibleRole.All);
        var notMatching = CreatePublishedFaq("profile.edit", VisibleRole.All);

        var result = resolver.Resolve("job.create", "JobSeeker", Language.En, [matching, notMatching]);
        result.Should().ContainSingle();
        result[0].Should().Be(matching.Id);
    }

    [Fact]
    public void ContextHelp_HelpArticleBeforeFaq()
    {
        var resolver = new ContextHelpResolver();
        var faq = CreatePublishedFaq("ctx", VisibleRole.All, FaqEntryKind.Faq);
        var helpArticle = CreatePublishedFaq("ctx", VisibleRole.All, FaqEntryKind.HelpArticle);

        var result = resolver.Resolve("ctx", "JobSeeker", Language.En, [faq, helpArticle]);
        result[0].Should().Be(helpArticle.Id);
    }

    [Fact]
    public void ContextHelp_RoleFiltering()
    {
        var resolver = new ContextHelpResolver();
        var employerOnly = CreatePublishedFaq("ctx", VisibleRole.Employer);

        var result = resolver.Resolve("ctx", "JobSeeker", Language.En, [employerOnly]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ContextHelp_AllRoleVisibleToEveryone()
    {
        var resolver = new ContextHelpResolver();
        var allRoles = CreatePublishedFaq("ctx", VisibleRole.All);

        var result = resolver.Resolve("ctx", "JobSeeker", Language.En, [allRoles]);
        result.Should().ContainSingle();
    }

    private static FaqEntry CreatePublishedFaq(string contextKey, VisibleRole role, FaqEntryKind kind = FaqEntryKind.Faq)
    {
        var content = FaqContent.Create("Q?", "<p>A</p>").Value;
        var entry = FaqEntry.CreateDraft(kind, Language.En, content);
        entry.SetContextKeys([contextKey]);
        var roles = VisibleRoleSet.Create([role]).Value;
        entry.SetVisibleRoles(roles);
        entry.Publish();
        return entry;
    }
}
