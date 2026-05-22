using FluentValidation;

namespace Nexhire.Modules.JobApplication.Core.JobApplications.Commands;

public sealed class AddBookmarkCommandValidator : AbstractValidator<AddBookmarkCommand>
{
    public AddBookmarkCommandValidator()
    {
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.JobPostingId).NotEmpty();
    }
}

public sealed class RemoveBookmarkCommandValidator : AbstractValidator<RemoveBookmarkCommand>
{
    public RemoveBookmarkCommandValidator()
    {
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.JobPostingId).NotEmpty();
    }
}

public sealed class SubmitApplicationCommandValidator : AbstractValidator<SubmitApplicationCommand>
{
    public SubmitApplicationCommandValidator()
    {
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.JobPostingId).NotEmpty();
        RuleFor(x => x.ResumeDocumentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        
        RuleFor(x => x.CoverLetter)
            .MaximumLength(4000)
            .When(x => x.CoverLetter != null);
    }
}

public sealed class WithdrawApplicationCommandValidator : AbstractValidator<WithdrawApplicationCommand>
{
    public WithdrawApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.ReasonCode).NotEmpty().Must(code =>
            code == "ChangedMind" ||
            code == "AcceptedAnotherOffer" ||
            code == "NoLongerInterested" ||
            code == "RoleNotAsExpected")
            .WithMessage("Invalid or non-seeker facing withdrawal reason code.");
            
        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => x.Comment != null);
    }
}
