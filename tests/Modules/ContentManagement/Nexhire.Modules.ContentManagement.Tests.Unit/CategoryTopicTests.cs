using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class CategoryTopicTests
{
    private static Dictionary<Language, string> MakeNames(string en = "News", string bn = "সংবাদ") =>
        new() { [Language.En] = en, [Language.Bn] = bn };

    [Fact]
    public void Category_Create_Valid_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "platform-news");
        cat.Slug.Should().Be("platform-news");
        cat.IsActive.Should().BeTrue();
        cat.Names[Language.En].Should().Be("News");
        cat.Names[Language.Bn].Should().Be("সংবাদ");
    }

    [Fact]
    public void Category_Rename_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "slug");
        cat.Rename(Language.En, "Updated News");
        cat.Names[Language.En].Should().Be("Updated News");
        cat.UpdatedOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Category_SetSlug_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "old-slug");
        cat.SetSlug("new-slug");
        cat.Slug.Should().Be("new-slug");
    }

    [Fact]
    public void Category_Deactivate_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "slug");
        cat.Deactivate();
        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Category_Activate_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "slug");
        cat.Deactivate();
        cat.Activate();
        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Category_EnsureDeletable_ZeroRefs_Succeeds()
    {
        var cat = Category.Create(MakeNames(), "slug");
        var result = cat.EnsureDeletable(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Category_EnsureDeletable_WithRefs_Fails()
    {
        var cat = Category.Create(MakeNames(), "slug");
        var result = cat.EnsureDeletable(5);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CATEGORY-IN-USE");
    }

    // Topic mirrors Category — same tests
    [Fact]
    public void Topic_Create_Valid_Succeeds()
    {
        var topic = Topic.Create(MakeNames("Laws", "আইন"), "laws");
        topic.Slug.Should().Be("laws");
        topic.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Topic_EnsureDeletable_ZeroRefs_Succeeds()
    {
        var topic = Topic.Create(MakeNames(), "slug");
        var result = topic.EnsureDeletable(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Topic_EnsureDeletable_WithRefs_Fails()
    {
        var topic = Topic.Create(MakeNames(), "slug");
        var result = topic.EnsureDeletable(3);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOPIC-IN-USE");
    }

    [Fact]
    public void Topic_Deactivate_Activate_Roundtrip()
    {
        var topic = Topic.Create(MakeNames(), "slug");
        topic.Deactivate();
        topic.IsActive.Should().BeFalse();
        topic.Activate();
        topic.IsActive.Should().BeTrue();
    }
}
