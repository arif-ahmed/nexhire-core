namespace Nexhire.Modules.Notification.Domain;

public enum Channel
{
    Email,
    InApp,
    Sms
}

public enum NotificationType
{
    JobRecommendation,
    ApplicationUpdate,
    Message,
    ProfileView,
    RecruiterActivity,
    Announcement,
    AccountSecurity,
    Transactional
}

public enum Priority
{
    High,
    Normal
}

public enum DeliveryStatus
{
    Pending,
    Queued,
    Sent,
    Delivered,
    Bounced,
    Failed,
    Complaint
}

public enum AttemptOutcome
{
    Succeeded,
    SoftBounce,
    HardBounce,
    ProviderError
}

public enum DigestWindow
{
    Daily,
    Weekly
}

public enum DigestStatus
{
    Open,
    Dispatched,
    Discarded
}

public enum ToastMode
{
    Toast,
    CenterOnly,
    Disabled
}

public enum Frequency
{
    Immediate,
    DailyDigest,
    WeeklyDigest
}
