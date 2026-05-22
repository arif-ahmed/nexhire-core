namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public enum ProfileStatus
{
    PendingActivation,
    Active,
    Deactivated
}

public enum ProfileVisibility
{
    Private,
    RecruitersOnly,
    Public
}

public enum SkillCategory
{
    Hard,
    Soft
}

public enum SkillTier
{
    Primary,
    Secondary
}

public enum DocumentKind
{
    Certificate,
    Portfolio,
    Reference,
    Other
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum ResumeParseStatus
{
    Uploaded,
    Scanning,
    Scanned,
    Parsing,
    Parsed,
    Failed
}

public enum HistoryAction
{
    Edited,
    Restored
}
