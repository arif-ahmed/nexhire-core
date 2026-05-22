using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class CanonicalSkillRef : ValueObject
{
    public string TaxonomyCode { get; }
    public string DisplayLabel { get; }

    private CanonicalSkillRef(string taxonomyCode, string displayLabel)
    {
        TaxonomyCode = taxonomyCode;
        DisplayLabel = displayLabel;
    }

    public static Result<CanonicalSkillRef> Create(string taxonomyCode, string displayLabel)
    {
        if (string.IsNullOrWhiteSpace(taxonomyCode))
        {
            return Result.Failure<CanonicalSkillRef>(new Error("CanonicalSkillRef.EmptyTaxonomyCode", "Taxonomy code is required."));
        }

        if (string.IsNullOrWhiteSpace(displayLabel))
        {
            return Result.Failure<CanonicalSkillRef>(new Error("CanonicalSkillRef.EmptyDisplayLabel", "Display label is required."));
        }

        return Result.Success(new CanonicalSkillRef(taxonomyCode.Trim(), displayLabel.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TaxonomyCode;
        yield return DisplayLabel;
    }
}
