using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class IntentHintTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenWorkFormatProvided()
    {
        var result = IntentHint.Create(workFormat: WorkFormat.Remote);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkFormat.Should().Be(WorkFormat.Remote);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenSkillTermsProvided()
    {
        var result = IntentHint.Create(skillTerms: ["C#", "SQL"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.SkillTerms.Should().BeEquivalentTo(["C#", "SQL"]);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenIndustriesProvided()
    {
        var result = IntentHint.Create(industries: ["IT", "Finance"]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSucceed_WhenLocationTermProvided()
    {
        var result = IntentHint.Create(locationTerm: "Dhaka");

        result.IsSuccess.Should().BeTrue();
        result.Value.LocationTerm.Should().Be("Dhaka");
    }

    [Fact]
    public void Create_ShouldFail_WhenAllFieldsEmpty()
    {
        var result = IntentHint.Create();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IntentHint.Empty");
    }
}
