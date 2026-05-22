namespace Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

public enum MatchFactor
{
    Skill,
    Education,
    Training,
    Location,
    Experience,
    Salary
}

public enum EducationLevel
{
    None,
    HighSchool,
    Diploma,
    Bachelor,
    Master,
    Phd
}

public enum ExperienceLevel
{
    Junior,
    Mid,
    Senior
}

public enum WorkArrangement
{
    OnSite,
    Hybrid,
    Remote
}

public enum PrivacyLevel
{
    Public,
    ApplyOnly,
    Hidden
}

public enum JobSearchStatus
{
    ActivelyLooking,
    OpenToOpportunities,
    Passive
}

public enum FeedbackSignal
{
    NotInterested,
    Viewed,
    Applied
}

public enum EmbeddingOwnerType
{
    Seeker,
    Posting
}

public enum PostingMatchStatus
{
    Active,
    Inactive
}

public enum NlpExtractionStatus
{
    Pending,
    Extracted,
    Failed
}

public enum ShortlistRefreshState
{
    Fresh,
    Refreshing,
    Stale
}

public enum ShortlistInclusionReason
{
    MatchAboveThreshold,
    AppliedDirectly
}

public enum TimeToProductivityEstimate
{
    OneWeek,
    TwoToThreeWeeks,
    FourPlusWeeks
}

public enum ContactLikelihood
{
    High,
    Medium,
    Low
}

public enum SalaryFitIndicator
{
    Green,
    Yellow,
    Red
}
