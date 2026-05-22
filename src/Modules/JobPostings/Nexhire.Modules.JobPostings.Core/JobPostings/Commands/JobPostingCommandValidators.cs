using FluentValidation;

namespace Nexhire.Modules.JobPostings.Core.JobPostings.Commands;

public sealed class CreateJobPostingCommandValidator : AbstractValidator<CreateJobPostingCommand>
{
    public CreateJobPostingCommandValidator()
    {
        RuleFor(x => x.EmployerId).NotEmpty();
        RuleFor(x => x.PostedByUserId).NotEmpty();
        RuleFor(x => x.Draft.Title).NotEmpty();
        RuleFor(x => x.Draft.Summary).NotEmpty();
        RuleFor(x => x.Draft.RequiredSkills).NotEmpty();
        RuleFor(x => x.Draft.DeadlineUtc).GreaterThan(DateTime.UtcNow);
    }
}

public sealed class PublishJobPostingCommandValidator : AbstractValidator<PublishJobPostingCommand>
{
    public PublishJobPostingCommandValidator()
    {
        RuleFor(x => x.JobPostingId).NotEmpty();
        RuleFor(x => x.EmployerId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class SuspendJobPostingCommandValidator : AbstractValidator<SuspendJobPostingCommand>
{
    public SuspendJobPostingCommandValidator()
    {
        RuleFor(x => x.JobPostingId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode("E-POST-REASON-REQUIRED");
    }
}

public sealed class RemoveJobPostingCommandValidator : AbstractValidator<RemoveJobPostingCommand>
{
    public RemoveJobPostingCommandValidator()
    {
        RuleFor(x => x.JobPostingId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode("E-POST-REASON-REQUIRED");
    }
}
