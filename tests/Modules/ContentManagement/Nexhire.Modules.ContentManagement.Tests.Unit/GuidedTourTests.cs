using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class GuidedTourTests
{
    private static AudienceSet DefaultAudience => AudienceSet.Create([Audience.NewUsers]).Value;

    [Fact]
    public void Create_Valid_Succeeds()
    {
        var tour = GuidedTour.Create(Language.En, "Onboarding", "Welcome tour", DefaultAudience);
        tour.Name.Should().Be("Onboarding");
        tour.IsActive.Should().BeTrue();
        tour.Steps.Should().BeEmpty();
    }

    [Fact]
    public void AddStep_Succeeds()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        var result = tour.AddStep("#btn-submit", "Click submit");
        result.IsSuccess.Should().BeTrue();
        tour.Steps.Should().HaveCount(1);
        tour.Steps[0].Order.Should().Be(1);
    }

    [Fact]
    public void AddStep_EmptySelector_Fails()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        var result = tour.AddStep("", "Tooltip");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOUR-SELECTOR-EMPTY");
    }

    [Fact]
    public void AddStep_EmptyTooltip_Fails()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        var result = tour.AddStep("#sel", "");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOUR-TOOLTIP-EMPTY");
    }

    [Fact]
    public void AddStep_TooltipTooLong_Fails()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        var result = tour.AddStep("#sel", new string('x', 501));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOUR-TOOLTIP-TOO-LONG");
    }

    [Fact]
    public void RemoveStep_Renumbers()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        tour.AddStep("#a", "Step 1");
        tour.AddStep("#b", "Step 2");
        tour.AddStep("#c", "Step 3");

        var stepToRemove = tour.Steps[0];
        tour.RemoveStep(stepToRemove.Id);

        tour.Steps.Should().HaveCount(2);
        tour.Steps.Select(s => s.Order).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void ReorderSteps_Succeeds()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        tour.AddStep("#a", "Step 1");
        tour.AddStep("#b", "Step 2");

        var reversed = tour.Steps.Select(s => s.Id).Reverse().ToList();
        var result = tour.ReorderSteps(reversed);
        result.IsSuccess.Should().BeTrue();
        tour.Steps[0].TargetSelector.Should().Be("#b");
        tour.Steps[1].TargetSelector.Should().Be("#a");
    }

    [Fact]
    public void ReorderSteps_NotPermutation_Fails()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        tour.AddStep("#a", "Step 1");
        tour.AddStep("#b", "Step 2");

        var result = tour.ReorderSteps([Guid.NewGuid()]);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOUR-NOT-PERMUTATION");
    }

    [Fact]
    public void UpdateStep_Succeeds()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        tour.AddStep("#a", "Old");
        var stepId = tour.Steps[0].Id;

        var action = TourAction.Create(TourActionKind.Navigate, "/jobs").Value;
        var result = tour.UpdateStep(stepId, "#b", "New", action);
        result.IsSuccess.Should().BeTrue();
        tour.Steps[0].TargetSelector.Should().Be("#b");
        tour.Steps[0].TooltipText.Should().Be("New");
    }

    [Fact]
    public void Deactivate_Activate_Roundtrip()
    {
        var tour = GuidedTour.Create(Language.En, "Tour", "Desc", DefaultAudience);
        tour.Deactivate();
        tour.IsActive.Should().BeFalse();
        tour.Activate();
        tour.IsActive.Should().BeTrue();
    }
}
