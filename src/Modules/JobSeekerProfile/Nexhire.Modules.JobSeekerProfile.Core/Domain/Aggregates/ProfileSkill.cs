using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class ProfileSkill : Entity<Guid>
{
    public CanonicalSkillRef CanonicalSkillRef { get; private set; } = null!;
    public string RawLabel { get; private set; } = null!;
    public SkillCategory Category { get; private set; }
    public SkillTier Tier { get; private set; }
    public int Proficiency { get; private set; }

    private ProfileSkill(Guid id, CanonicalSkillRef canonicalSkillRef, string rawLabel, SkillCategory category, SkillTier tier, int proficiency) : base(id)
    {
        CanonicalSkillRef = canonicalSkillRef;
        RawLabel = rawLabel;
        Category = category;
        Tier = tier;
        Proficiency = proficiency;
    }

    private ProfileSkill()
    {
        // Required by EF Core
    }

    public static Result<ProfileSkill> Create(Guid id, CanonicalSkillRef canonicalSkillRef, string rawLabel, SkillCategory category, SkillTier tier, int proficiency)
    {
        if (canonicalSkillRef == null)
        {
            return Result.Failure<ProfileSkill>(new Error("ProfileSkill.NullCanonicalRef", "Canonical skill reference is required."));
        }

        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return Result.Failure<ProfileSkill>(new Error("ProfileSkill.EmptyRawLabel", "Raw skill label is required."));
        }

        if (proficiency < 1 || proficiency > 5)
        {
            return Result.Failure<ProfileSkill>(new Error("ProfileSkill.InvalidProficiency", "Proficiency must be between 1 and 5."));
        }

        return Result.Success(new ProfileSkill(id, canonicalSkillRef, rawLabel.Trim(), category, tier, proficiency));
    }

    public void UpdateProficiency(int proficiency)
    {
        Proficiency = proficiency;
    }
}
