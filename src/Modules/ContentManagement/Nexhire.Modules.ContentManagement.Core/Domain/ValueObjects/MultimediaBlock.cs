using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class MultimediaBlock : ValueObject
{
    public MediaBlockKind BlockKind { get; }
    public MediaReference? Media { get; }
    public IReadOnlyList<GuideStep> Steps { get; }

    private MultimediaBlock(MediaBlockKind blockKind, MediaReference? media, IReadOnlyList<GuideStep> steps)
    {
        BlockKind = blockKind;
        Media = media;
        Steps = steps;
    }

    public static Result<MultimediaBlock> CreateVideo(MediaReference media)
    {
        if (media.Kind != MediaKind.Video)
            return Result.Failure<MultimediaBlock>(new Error("E-MULTIMEDIA-KIND-MISMATCH", "Video block requires a video media reference."));

        return Result.Success(new MultimediaBlock(MediaBlockKind.Video, media, []));
    }

    public static Result<MultimediaBlock> CreateStepGuide(IReadOnlyList<GuideStep> steps)
    {
        if (steps.Count == 0)
            return Result.Failure<MultimediaBlock>(new Error("E-MULTIMEDIA-NO-STEPS", "Step guide must have at least one step."));

        return Result.Success(new MultimediaBlock(MediaBlockKind.StepGuide, null, steps));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BlockKind;
    }
}

public enum MediaBlockKind
{
    Video = 0,
    StepGuide = 1
}

public sealed class GuideStep : ValueObject
{
    public int Order { get; }
    public string Caption { get; }
    public MediaReference? Image { get; }

    private GuideStep(int order, string caption, MediaReference? image)
    {
        Order = order;
        Caption = caption;
        Image = image;
    }

    public static Result<GuideStep> Create(int order, string caption, MediaReference? image = null)
    {
        if (string.IsNullOrWhiteSpace(caption))
            return Result.Failure<GuideStep>(new Error("E-GUIDE-STEP-CAPTION-EMPTY", "Step caption cannot be empty."));

        if (image is not null && image.Kind != MediaKind.Image)
            return Result.Failure<GuideStep>(new Error("E-GUIDE-STEP-IMAGE-KIND", "Step image must be of Image kind."));

        return Result.Success(new GuideStep(order, caption.Trim(), image));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Order;
        yield return Caption;
    }
}

public sealed class TourAction : ValueObject
{
    public TourActionKind Kind { get; }
    public string? Payload { get; }

    private TourAction(TourActionKind kind, string? payload)
    {
        Kind = kind;
        Payload = payload;
    }

    public static Result<TourAction> Create(TourActionKind kind, string? payload = null)
    {
        if (kind == TourActionKind.Navigate && string.IsNullOrWhiteSpace(payload))
            return Result.Failure<TourAction>(new Error("E-TOUR-ACTION-NO-PAYLOAD", "Navigate action requires a target route payload."));

        return Result.Success(new TourAction(kind, kind == TourActionKind.Navigate ? payload!.Trim() : null));
    }

    public static TourAction None() => new(TourActionKind.None, null);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Kind;
        yield return Payload ?? string.Empty;
    }
}
