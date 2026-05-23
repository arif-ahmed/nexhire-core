using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class TourStep : Entity<Guid>
{
    public int Order { get; private set; }
    public string TargetSelector { get; private set; } = string.Empty;
    public string TooltipText { get; private set; } = string.Empty;
    public TourAction? Action { get; private set; }

    private TourStep() { }

    private TourStep(Guid id, int order, string targetSelector, string tooltipText, TourAction? action) : base(id)
    {
        Order = order;
        TargetSelector = targetSelector;
        TooltipText = tooltipText;
        Action = action;
    }

    internal static TourStep Create(int order, string targetSelector, string tooltipText, TourAction? action)
    {
        return new TourStep(Guid.NewGuid(), order, targetSelector, tooltipText, action);
    }

    internal void Update(string targetSelector, string tooltipText, TourAction? action)
    {
        TargetSelector = targetSelector;
        TooltipText = tooltipText;
        Action = action;
    }

    internal void SetOrder(int order) => Order = order;
}

public sealed class GuidedTour : AggregateRoot<Guid>
{
    private readonly List<TourStep> _steps = new();

    public Language Language { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AudienceSet TargetAudience { get; private set; } = null!;
    public IReadOnlyList<TourStep> Steps => _steps.OrderBy(s => s.Order).ToList().AsReadOnly();
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private GuidedTour() : base() { }

    private GuidedTour(Guid id, Language language, string name, string description, AudienceSet audience) : base(id)
    {
        Language = language;
        Name = name;
        Description = description;
        TargetAudience = audience;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static GuidedTour Create(Language language, string name, string description, AudienceSet audience)
    {
        return new GuidedTour(Guid.NewGuid(), language, name, description, audience);
    }

    public Result AddStep(string targetSelector, string tooltipText, TourAction? action = null)
    {
        if (string.IsNullOrWhiteSpace(targetSelector))
            return Result.Failure(new Error("E-TOUR-SELECTOR-EMPTY", "Target selector cannot be empty."));

        if (string.IsNullOrWhiteSpace(tooltipText))
            return Result.Failure(new Error("E-TOUR-TOOLTIP-EMPTY", "Tooltip text cannot be empty."));

        if (tooltipText.Length > 500)
            return Result.Failure(new Error("E-TOUR-TOOLTIP-TOO-LONG", "Tooltip text cannot exceed 500 characters."));

        var order = _steps.Count + 1;
        var step = TourStep.Create(order, targetSelector.Trim(), tooltipText.Trim(), action);
        _steps.Add(step);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateStep(Guid stepId, string targetSelector, string tooltipText, TourAction? action)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null)
            return Result.Failure(new Error("E-TOUR-STEP-NOT-FOUND", "Step not found."));

        step.Update(targetSelector.Trim(), tooltipText.Trim(), action);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null)
            return Result.Failure(new Error("E-TOUR-STEP-NOT-FOUND", "Step not found."));

        _steps.Remove(step);
        RenumberSteps();
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ReorderSteps(IEnumerable<Guid> orderedStepIds)
    {
        var idList = orderedStepIds.ToList();
        var existingIds = _steps.Select(s => s.Id).ToHashSet();

        if (idList.Count != existingIds.Count || !idList.All(id => existingIds.Contains(id)))
            return Result.Failure(new Error("E-TOUR-NOT-PERMUTATION", "Provided step IDs must be a permutation of existing step IDs."));

        for (int i = 0; i < idList.Count; i++)
        {
            var step = _steps.First(s => s.Id == idList[i]);
            step.SetOrder(i + 1);
        }

        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    private void RenumberSteps()
    {
        var ordered = _steps.OrderBy(s => s.Order).ToList();
        for (int i = 0; i < ordered.Count; i++)
            ordered[i].SetOrder(i + 1);
    }
}
