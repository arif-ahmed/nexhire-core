using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Events;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class HelpFeedback : AggregateRoot<Guid>
{
    public Guid FaqEntryId { get; private set; }
    public bool WasHelpful { get; private set; }
    public FeedbackReason? Reason { get; private set; }
    public string? Comment { get; private set; }
    public string? SubmittedByRole { get; private set; }
    public Language Language { get; private set; }
    public DateTime SubmittedOnUtc { get; private set; }

    private HelpFeedback() : base() { }

    private HelpFeedback(
        Guid id,
        Guid faqEntryId,
        bool wasHelpful,
        FeedbackReason? reason,
        string? comment,
        string? submittedByRole,
        Language language) : base(id)
    {
        FaqEntryId = faqEntryId;
        WasHelpful = wasHelpful;
        Reason = reason;
        Comment = comment;
        SubmittedByRole = submittedByRole;
        Language = language;
        SubmittedOnUtc = DateTime.UtcNow;
    }

    public static Result<HelpFeedback> Submit(
        Guid faqEntryId,
        bool wasHelpful,
        FeedbackReason? reason,
        string? comment,
        string? submittedByRole,
        Language language)
    {
        if (!wasHelpful && reason is null)
            return Result.Failure<HelpFeedback>(new Error("E-FEEDBACK-REASON-REQUIRED", "Reason is required when feedback is not helpful."));

        if (wasHelpful && reason is not null)
            return Result.Failure<HelpFeedback>(new Error("E-FEEDBACK-REASON-FORBIDDEN", "Reason must not be set when feedback is helpful."));

        if (comment is not null && comment.Length > 2000)
            return Result.Failure<HelpFeedback>(new Error("E-FEEDBACK-COMMENT-TOO-LONG", "Comment cannot exceed 2000 characters."));

        var feedback = new HelpFeedback(Guid.NewGuid(), faqEntryId, wasHelpful, reason, comment, submittedByRole, language);
        feedback.RaiseDomainEvent(new HelpFeedbackReceivedDomainEvent(feedback.Id, faqEntryId, wasHelpful, reason));

        return Result.Success(feedback);
    }
}
