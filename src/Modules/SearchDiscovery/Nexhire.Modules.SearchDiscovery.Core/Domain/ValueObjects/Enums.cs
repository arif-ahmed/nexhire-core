namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public enum EmploymentType
{
    FullTime,
    PartTime,
    Contract,
    Internship,
    Temporary
}

public enum WorkFormat
{
    OnSite,
    Hybrid,
    Remote
}

public enum SortOption
{
    Relevance,
    MatchScore,
    DatePosted,
    Salary,
    ApplicationDeadline
}

public enum NotificationPreference
{
    None,
    DailyDigest,
    WeeklyDigest,
    Instant
}
