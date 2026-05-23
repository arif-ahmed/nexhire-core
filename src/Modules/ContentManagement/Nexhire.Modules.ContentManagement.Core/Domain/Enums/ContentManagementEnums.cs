namespace Nexhire.Modules.ContentManagement.Core.Domain.Enums;

public enum Language
{
    En = 0,
    Bn = 1
}

public enum ArticleStatus
{
    Draft = 0,
    Scheduled = 1,
    Published = 2,
    Unpublished = 3,
    Archived = 4
}

public enum ContentStatus
{
    Draft = 0,
    Published = 1
}

public enum FaqEntryKind
{
    Faq = 0,
    HelpArticle = 1
}

public enum FeedbackReason
{
    Unclear = 0,
    Incomplete = 1,
    Incorrect = 2,
    Other = 3
}

public enum MediaKind
{
    Image = 0,
    Video = 1
}

public enum TourActionKind
{
    None = 0,
    Click = 1,
    Navigate = 2
}

public enum Audience
{
    NewUsers = 0,
    JobSeekers = 1,
    Recruiters = 2,
    Administrators = 3
}

public enum VisibleRole
{
    JobSeeker = 0,
    Employer = 1,
    Administrator = 2,
    All = 3
}
