using FluentValidation;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Commands;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Validators;

public sealed class AddTaxonomyTermCommandValidator : AbstractValidator<AddTaxonomyTermCommand>
{
    public AddTaxonomyTermCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false)
            .WithMessage("Taxonomy kind must be one of: Skills, Occupations, TrainingPrograms.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Term code is required.")
            .MaximumLength(64).WithMessage("Term code cannot exceed 64 characters.")
            .Must(c => c != null && c.Contains('.'))
            .WithMessage("Term code must contain a dot separator (e.g. SKILL.PYTHON).");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Display label is required.")
            .MaximumLength(200).WithMessage("Display label cannot exceed 200 characters.");

        // Conditional shape rules:
        // Skills requires a category. Non-skills rejects category.
        RuleFor(x => x.Category)
            .Custom((category, context) =>
            {
                var command = context.InstanceToValidate;
                if (Enum.TryParse<TaxonomyKind>(command.Kind, true, out var kind))
                {
                    if (kind == TaxonomyKind.Skills)
                    {
                        if (string.IsNullOrWhiteSpace(category))
                        {
                            context.AddFailure("Category", "Skill category is required for Skills taxonomy (Hard or Soft).");
                        }
                        else if (!Enum.TryParse<SkillCategory>(category, true, out _))
                        {
                            context.AddFailure("Category", "Skill category must be either Hard or Soft.");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(category))
                        {
                            context.AddFailure("Category", "Category must be null or empty for non-skills taxonomies.");
                        }
                    }
                }
            });
    }
}

public sealed class RenameTaxonomyTermCommandValidator : AbstractValidator<RenameTaxonomyTermCommand>
{
    public RenameTaxonomyTermCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        RuleFor(x => x.NewLabel)
            .NotEmpty().WithMessage("New label is required.")
            .MaximumLength(200).WithMessage("New label cannot exceed 200 characters.");
    }
}

public sealed class RecategorizeSkillCommandValidator : AbstractValidator<RecategorizeSkillCommand>
{
    public RecategorizeSkillCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        RuleFor(x => x.NewCategory)
            .NotEmpty().WithMessage("New category is required.")
            .IsEnumName(typeof(SkillCategory), false)
            .WithMessage("New category must be either Hard or Soft.");
    }
}

public sealed class ReparentTaxonomyTermCommandValidator : AbstractValidator<ReparentTaxonomyTermCommand>
{
    public ReparentTaxonomyTermCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");
    }
}

public sealed class DeprecateTaxonomyTermCommandValidator : AbstractValidator<DeprecateTaxonomyTermCommand>
{
    public DeprecateTaxonomyTermCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        RuleFor(x => x.ReplacedByCode)
            .Must((command, replacedByCode) => string.IsNullOrWhiteSpace(replacedByCode) || replacedByCode.Trim().ToUpperInvariant() != command.Code.Trim().ToUpperInvariant())
            .WithMessage("A term cannot be replaced by itself.");
    }
}

public sealed class ReactivateTaxonomyTermCommandValidator : AbstractValidator<ReactivateTaxonomyTermCommand>
{
    public ReactivateTaxonomyTermCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");
    }
}

public sealed class SeedTaxonomiesCommandValidator : AbstractValidator<SeedTaxonomiesCommand>
{
}

public sealed class BulkImportTaxonomyCommandValidator : AbstractValidator<BulkImportTaxonomyCommand>
{
    public BulkImportTaxonomyCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsEnumName(typeof(TaxonomyKind), false)
            .WithMessage("Taxonomy kind must be one of: Skills, Occupations, TrainingPrograms.");

        RuleFor(x => x.CsvStream)
            .NotNull().WithMessage("CSV file stream is required.");
    }
}
