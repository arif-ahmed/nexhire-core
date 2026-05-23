using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;

public sealed class TaxonomyTerm : Entity<Guid>
{
    public TermCode Code { get; private set; } = null!;
    public string Label { get; private set; } = string.Empty;
    public SkillCategory? Category { get; private set; }
    public TermCode? ParentCode { get; private set; }
    public TermStatus Status { get; private set; }
    public TermCode? ReplacedByCode { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? DeprecatedOnUtc { get; private set; }

    private TaxonomyTerm() : base()
    {
        // Required by EF Core
    }

    private TaxonomyTerm(
        Guid id,
        TermCode code,
        string label,
        SkillCategory? category,
        TermCode? parentCode,
        TermStatus status,
        DateTime createdOnUtc) : base(id)
    {
        Code = code;
        Label = label;
        Category = category;
        ParentCode = parentCode;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UsageCount = 0;
    }

    public static TaxonomyTerm Create(
        Guid id,
        TermCode code,
        string label,
        SkillCategory? category,
        TermCode? parentCode)
    {
        return new TaxonomyTerm(
            id,
            code,
            label,
            category,
            parentCode,
            TermStatus.Active,
            DateTime.UtcNow);
    }

    public void UpdateLabel(string newLabel)
    {
        if (string.IsNullOrWhiteSpace(newLabel))
        {
            throw new ArgumentException("Label cannot be empty.", nameof(newLabel));
        }
        if (newLabel.Length > 200)
        {
            throw new ArgumentException("Label cannot exceed 200 characters.", nameof(newLabel));
        }
        Label = newLabel;
    }

    public void UpdateCategory(SkillCategory? category)
    {
        Category = category;
    }

    public void UpdateParent(TermCode? newParentCode)
    {
        ParentCode = newParentCode;
    }

    public void Deprecate(TermCode? replacedByCode)
    {
        Status = TermStatus.Deprecated;
        ReplacedByCode = replacedByCode;
        DeprecatedOnUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = TermStatus.Active;
        ReplacedByCode = null;
        DeprecatedOnUtc = null;
    }

    public void ModifyUsageCount(int delta)
    {
        UsageCount = Math.Max(0, UsageCount + delta);
    }
}
