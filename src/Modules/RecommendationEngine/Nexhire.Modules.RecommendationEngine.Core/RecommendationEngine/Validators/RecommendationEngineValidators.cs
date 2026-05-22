using FluentValidation;
using Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Commands;
using Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Queries;

namespace Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Validators;

public sealed class UpdateMatchThresholdCommandValidator : AbstractValidator<UpdateMatchThresholdCommand>
{
    public UpdateMatchThresholdCommandValidator()
    {
        RuleFor(x => x.NewThreshold).InclusiveBetween(0, 100)
            .WithErrorCode("E-THRESHOLD-OUT-OF-RANGE").WithMessage("Threshold must be between 0 and 100.");
        RuleFor(x => x.AdminId).NotEmpty();
    }
}

public sealed class SetPerPostingThresholdCommandValidator : AbstractValidator<SetPerPostingThresholdCommand>
{
    public SetPerPostingThresholdCommandValidator()
    {
        RuleFor(x => x.PostingId).NotEmpty();
        RuleFor(x => x.Percent).Must(v => v == null || (v >= 0 && v <= 100))
            .WithErrorCode("E-THRESHOLD-OUT-OF-RANGE").WithMessage("Threshold must be null or between 0 and 100.");
        RuleFor(x => x.ChangedBy).NotEmpty();
    }
}

public sealed class CreateWeightVariantCommandValidator : AbstractValidator<CreateWeightVariantCommand>
{
    public CreateWeightVariantCommandValidator()
    {
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.Skill).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Education).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Training).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Location).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Experience).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Salary).InclusiveBetween(0m, 1m);
        RuleFor(x => x.AllocationPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x).Must(x => Math.Abs(x.Skill + x.Education + x.Training + x.Location + x.Experience + x.Salary - 1.0m) <= 0.01m)
            .WithErrorCode("E-WEIGHTS-INVALID-SUM").WithMessage("Factor weights must sum to 1.0 (±0.01).");
    }
}

public sealed class UpdateMatchingWeightsCommandValidator : AbstractValidator<UpdateMatchingWeightsCommand>
{
    public UpdateMatchingWeightsCommandValidator()
    {
        RuleFor(x => x.Skill).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Education).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Training).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Location).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Experience).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Salary).InclusiveBetween(0m, 1m);
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x).Must(x => Math.Abs(x.Skill + x.Education + x.Training + x.Location + x.Experience + x.Salary - 1.0m) <= 0.01m)
            .WithErrorCode("E-WEIGHTS-INVALID-SUM").WithMessage("Factor weights must sum to 1.0 (±0.01).");
    }
}

public sealed class RollbackWeightProfileCommandValidator : AbstractValidator<RollbackWeightProfileCommand>
{
    public RollbackWeightProfileCommandValidator()
    {
        RuleFor(x => x.TargetVersion).NotEmpty()
            .WithErrorCode("E-WEIGHTS-ROLLBACK-UNKNOWN").WithMessage("Target version is required.");
    }
}

public sealed class SetQualificationThresholdCommandValidator : AbstractValidator<SetQualificationThresholdCommand>
{
    public SetQualificationThresholdCommandValidator()
    {
        RuleFor(x => x.JobPostingId).NotEmpty();
        RuleFor(x => x.RecruiterId).NotEmpty();
        RuleFor(x => x.MinOverallMatch).InclusiveBetween(0, 100);
        RuleFor(x => x.MinSkillMatch).InclusiveBetween(0, 100);
        RuleFor(x => x.MinExperienceYears).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateTalentPoolCommandValidator : AbstractValidator<CreateTalentPoolCommand>
{
    public CreateTalentPoolCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
        RuleFor(x => x.EmployerId).NotEmpty();
        RuleFor(x => x.RecruiterId).NotEmpty();
    }
}

public sealed class AddCandidateToTalentPoolCommandValidator : AbstractValidator<AddCandidateToTalentPoolCommand>
{
    public AddCandidateToTalentPoolCommandValidator()
    {
        RuleFor(x => x.TalentPoolId).NotEmpty();
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.RecruiterId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(2000).When(x => x.Note != null);
    }
}

public sealed class UpdateTalentPoolCandidateNoteCommandValidator : AbstractValidator<UpdateTalentPoolCandidateNoteCommand>
{
    public UpdateTalentPoolCandidateNoteCommandValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.JobSeekerId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(2000).When(x => x.Note != null);
    }
}

public sealed class SearchCandidatesQueryValidator : AbstractValidator<SearchCandidatesQuery>
{
    public SearchCandidatesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class SetShortlistSizeCommandValidator : AbstractValidator<SetShortlistSizeCommand>
{
    public SetShortlistSizeCommandValidator()
    {
        RuleFor(x => x.PostingId).NotEmpty();
        RuleFor(x => x.Size).GreaterThan(0)
            .WithMessage("Shortlist size must be greater than zero.");
    }
}
