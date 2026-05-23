namespace Nexhire.Modules.Reporting.Core.Domain.Enums;

public enum ActorRole { JobSeeker, Employer, Administrator, System }

public enum ActivityType
{
    Login, LoginFailed, ProfileView, JobSearch, JobApplication, JobBookmark, JobUnbookmark,
    JobPostingCreated, JobPostingPublished, JobPostingUpdated, JobPostingExpired, JobPostingClosed,
    JobPostingSuspended, JobPostingReinstated, CandidateSearch, ApplicationReview, ApplicationStatusChanged,
    ApplicationWithdrawn, ReportViewed, ReportDownloaded, UserRegistered, AccountActivated,
    AccountSuspended, AccountReinstated, AccountDeactivated, PasswordReset, RoleAssigned,
    EmployerRegistered, EmployerProfileUpdated, EmployerVerificationRequested, EmployerVerified,
    EmployerVerificationFailed, CandidateSaved, JobSeekerRegistered, ProfileCompleted,
    ResumeUploaded, ResumeParsed, ProfileSkillsUpdated, ProfileVisibilityChanged, DocumentUploaded,
    ProfileCompletenessChanged, SavedSearchCreated, SavedSearchMatch, EmbeddingsRefreshed,
    MatchThresholdChanged, ExternalJobIngested, ExternalJobUpdated, ExternalJobRetracted,
    IdentityVerified, IdentityVerificationFailed, EducationVerified, EmployerVerifiedByGovernment,
    SyncError, SyncReconciled, NotificationPreferencesUpdated, TaxonomyUpdated, ArticlePublished,
    ArticleScheduled, ArticleArchived, FAQPublished
}

public enum ReportDefinitionKind { Template, Custom }

public enum ReportCategory
{
    EmploymentStats, ActivityReports, Performance, IndustryAnalytics, SkillDemand, Outcomes, Custom
}

public enum ReportRunStatus { Queued, Running, Completed, Failed }

public enum ExportFormat { Pdf, Xlsx, Csv }

public enum ScheduleStatus { Active, Paused }

public enum RetentionAction { Archive, HardDelete }

public enum RetentionPolicyStatus { Active, Archived }

public enum ReportDefinitionStatus { Active, Archived }

public enum AlertSeverity { Critical, Warning, Info }

public enum AlertChannel { Email, InApp }

public enum AlertRuleStatus { Enabled, Disabled }

public enum VisualizationType { Table, BarChart, LineChart, PieChart, Heatmap }

public enum FilterOperator { Eq, In, Between, Gte, Lte }

public enum ParameterKind { DateRange, FilterDropdown, MetricSelection }

public enum RollupGrain { Day, Week, Month }

public enum IncidentTrigger { ThresholdBreach, Anomaly }

public enum IncidentState { Raised, Acknowledged, Suppressed, Escalated }

public enum TriggerMode { OnDemand, Scheduled }

public enum Frequency { Daily, Weekly, Monthly, Quarterly }

public enum Comparator { GreaterThan, LessThan }
