using FluentValidation;
using Nexhire.Modules.ContentManagement.Core.Application.Commands;

namespace Nexhire.Modules.ContentManagement.Core.Application.Validators;

public sealed class CreateArticleDraftCommandValidator : AbstractValidator<CreateArticleDraftCommand>
{
    public CreateArticleDraftCommandValidator()
    {
        RuleFor(x => x.Language).NotEmpty().Must(BeValidLanguage).WithMessage("Language must be 'En' or 'Bn'.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(500);
        RuleFor(x => x.BodyRichText).NotEmpty();
    }
    private static bool BeValidLanguage(string lang) => lang is "En" or "Bn" or "en" or "bn";
}

public sealed class UpdateArticleContentCommandValidator : AbstractValidator<UpdateArticleContentCommand>
{
    public UpdateArticleContentCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(500);
        RuleFor(x => x.BodyRichText).NotEmpty();
    }
}

public sealed class RemoveArticleLocalizationCommandValidator : AbstractValidator<RemoveArticleLocalizationCommand>
{
    public RemoveArticleLocalizationCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty();
    }
}

public sealed class SetArticleCategoryCommandValidator : AbstractValidator<SetArticleCategoryCommand>
{
    public SetArticleCategoryCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class SetArticleTagsCommandValidator : AbstractValidator<SetArticleTagsCommand>
{
    public SetArticleTagsCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Tags).NotNull();
        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Tags.Count).LessThanOrEqualTo(25);
    }
}

public sealed class AddArticleMediaCommandValidator : AbstractValidator<AddArticleMediaCommand>
{
    private static readonly HashSet<string> ImageMimes = ["image/jpeg", "image/png", "image/gif"];
    private static readonly HashSet<string> VideoMimes = ["video/mp4", "video/webm"];

    public AddArticleMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.StorageKey).NotEmpty();
        RuleFor(x => x.MimeType).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Kind).Must(BeValidKind).WithMessage("Kind must be 'Image' or 'Video'.");
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            if (cmd.Kind.Equals("Image", StringComparison.OrdinalIgnoreCase) && !ImageMimes.Contains(cmd.MimeType))
                ctx.AddFailure("MimeType", "Image MIME type must be jpeg, png, or gif.");
            if (cmd.Kind.Equals("Video", StringComparison.OrdinalIgnoreCase) && !VideoMimes.Contains(cmd.MimeType))
                ctx.AddFailure("MimeType", "Video MIME type must be mp4 or webm.");
        });
    }
    private static bool BeValidKind(string kind) => kind is "Image" or "Video" or "image" or "video";
}

public sealed class RemoveArticleMediaCommandValidator : AbstractValidator<RemoveArticleMediaCommand>
{
    public RemoveArticleMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.StorageKey).NotEmpty();
    }
}

public sealed class PublishArticleCommandValidator : AbstractValidator<PublishArticleCommand>
{
    public PublishArticleCommandValidator() => RuleFor(x => x.ArticleId).NotEmpty();
}

public sealed class ScheduleArticleCommandValidator : AbstractValidator<ScheduleArticleCommand>
{
    public ScheduleArticleCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.PublishAtUtc).GreaterThan(DateTime.UtcNow).WithMessage("Publication time must be in the future.");
    }
}

public sealed class BulkArchiveArticlesCommandValidator : AbstractValidator<BulkArchiveArticlesCommand>
{
    public BulkArchiveArticlesCommandValidator()
    {
        RuleFor(x => x.ArticleIds).NotNull().Must(ids => ids.Count > 0 && ids.Count <= 50)
            .WithMessage("Must provide between 1 and 50 article IDs.");
    }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Names).NotNull().Must(n => n.Count > 0).WithMessage("At least one name is required.");
    }
}

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Names).NotNull().Must(n => n.Count > 0).WithMessage("At least one name is required.");
    }
}

public sealed class CreateFaqEntryCommandValidator : AbstractValidator<CreateFaqEntryCommand>
{
    public CreateFaqEntryCommandValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().Must(k => k is "Faq" or "HelpArticle");
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Question).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AnswerRichText).NotEmpty();
    }
}

public sealed class SetFaqVisibleRolesCommandValidator : AbstractValidator<SetFaqVisibleRolesCommand>
{
    public SetFaqVisibleRolesCommandValidator()
    {
        RuleFor(x => x.FaqEntryId).NotEmpty();
        RuleFor(x => x.Roles).NotNull().Must(r => r.Count > 0).WithMessage("At least one role is required.");
    }
}

public sealed class SubmitHelpFeedbackCommandValidator : AbstractValidator<SubmitHelpFeedbackCommand>
{
    public SubmitHelpFeedbackCommandValidator()
    {
        RuleFor(x => x.FaqEntryId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment is not null);
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            if (!cmd.WasHelpful && string.IsNullOrEmpty(cmd.Reason))
                ctx.AddFailure("Reason", "Reason is required when feedback is not helpful.");
            if (cmd.WasHelpful && !string.IsNullOrEmpty(cmd.Reason))
                ctx.AddFailure("Reason", "Reason must not be set when feedback is helpful.");
        });
    }
}

public sealed class CreateGuidedTourCommandValidator : AbstractValidator<CreateGuidedTourCommand>
{
    public CreateGuidedTourCommandValidator()
    {
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.TargetAudience).NotNull().Must(a => a.Count > 0).WithMessage("At least one audience is required.");
    }
}

public sealed class AddTourStepCommandValidator : AbstractValidator<AddTourStepCommand>
{
    public AddTourStepCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.TargetSelector).NotEmpty();
        RuleFor(x => x.TooltipText).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateContentPreferenceCommandValidator : AbstractValidator<UpdateContentPreferenceCommand>
{
    public UpdateContentPreferenceCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
