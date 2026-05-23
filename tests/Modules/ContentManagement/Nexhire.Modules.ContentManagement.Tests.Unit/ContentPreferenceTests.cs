using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class ContentPreferenceTests
{
    [Fact]
    public void CreateDefault_SetsDefaults()
    {
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());
        pref.PreferredLanguage.Should().Be(Language.En);
        pref.IncludedCategoryIds.Should().BeEmpty();
        pref.HiddenCategoryIds.Should().BeEmpty();
    }

    [Fact]
    public void SetPreferredLanguage_Succeeds()
    {
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());
        pref.SetPreferredLanguage(Language.Bn);
        pref.PreferredLanguage.Should().Be(Language.Bn);
    }

    [Fact]
    public void SetIncludedCategories_Succeeds()
    {
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());
        var c1 = Guid.NewGuid();
        var result = pref.SetIncludedCategories([c1]);
        result.IsSuccess.Should().BeTrue();
        pref.IncludedCategoryIds.Should().Contain(c1);
    }

    [Fact]
    public void SetHiddenCategories_OverlapWithIncluded_Fails()
    {
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());
        var c1 = Guid.NewGuid();
        pref.SetIncludedCategories([c1]);
        var result = pref.SetHiddenCategories([c1]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-PREFERENCE-OVERLAP");
    }

    [Fact]
    public void SetIncludedCategories_OverlapWithHidden_Fails()
    {
        var pref = ContentPreference.CreateDefault(Guid.NewGuid());
        var c1 = Guid.NewGuid();
        pref.SetHiddenCategories([c1]);
        var result = pref.SetIncludedCategories([c1]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-PREFERENCE-OVERLAP");
    }
}
