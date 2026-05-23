using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Reporting.Core.Domain.Services;

public record BaselineWindow(decimal Mean, decimal StdDev, int SampleCount);
public record AnomalyVerdict(bool IsAnomaly, decimal DeviationSigma);

public static class RetentionCutoffCalculator
{
    public static DateTime ComputeCutoff(RetentionPolicy policy, DateTime nowUtc)
        => nowUtc.AddDays(-policy.RetentionDays);

    public static DateTime ComputeWarningDate(RetentionPolicy policy, DateTime nowUtc)
        => ComputeCutoff(policy, nowUtc).AddDays(policy.WarningDays);
}

public static class AnomalyDetector
{
    private const double DefaultSigmaThreshold = 3.0;

    public static AnomalyVerdict Evaluate(string metricKey, decimal observedValue, BaselineWindow baseline, double sigmaThreshold = DefaultSigmaThreshold)
    {
        if (baseline.StdDev == 0) return new AnomalyVerdict(false, 0);
        var sigma = Math.Abs((double)(observedValue - baseline.Mean)) / (double)baseline.StdDev;
        return new AnomalyVerdict(sigma > sigmaThreshold, (decimal)sigma);
    }
}

public record ReportFilterSet(List<ReportFilter> Filters, IReadOnlySet<string> MaskedFields);

public static class ReportDataScopeFilter
{
    private static readonly HashSet<string> _salaryFields = new() { "salary_min", "salary_max", "salary_median", "salary_p25", "salary_p75" };
    private static readonly HashSet<string> _piiFields = new() { "applicant_name", "email", "phone" };

    public static ReportFilterSet ApplyRoleScope(ReportSpec spec, RoleScope roleScope)
    {
        var filters = new List<ReportFilter>(spec.Filters);
        if (roleScope.Role.Value == "EmployerOwner" && roleScope.EmployerId.HasValue)
        {
            var employerFilter = ReportFilter.Create("employer_id", FilterOperator.Eq, new List<string> { roleScope.EmployerId.Value.ToString() });
            if (employerFilter.IsSuccess) filters.Add(employerFilter.Value);
        }
        return new ReportFilterSet(filters, MaskedFieldsFor(roleScope, ReportCategory.Custom));
    }

    public static IReadOnlySet<string> MaskedFieldsFor(RoleScope roleScope, ReportCategory category)
    {
        if (roleScope.Role.Value is "SystemAdministrator" or "MoLAdministrator" or "Auditor")
            return new HashSet<string>();
        if (roleScope.Role.Value == "EmployerOwner")
        {
            var masked = new HashSet<string>(_piiFields);
            masked.UnionWith(_salaryFields);
            return masked;
        }
        return new HashSet<string>();
    }
}

public static class ScheduleNextRunCalculator
{
    public static DateTime ComputeNextRun(ScheduleCadence cadence, DateTime afterUtc)
    {
        var candidate = cadence.Frequency switch
        {
            Frequency.Daily => afterUtc.Date.AddDays(1).Add(cadence.TimeOfDayUtc.ToTimeSpan()),
            Frequency.Weekly => NextWeekday(afterUtc, cadence.DayOfWeek!.Value, cadence.TimeOfDayUtc),
            Frequency.Monthly => NextMonthly(afterUtc, cadence.DayOfMonth!.Value, cadence.TimeOfDayUtc),
            Frequency.Quarterly => NextQuarterly(afterUtc, cadence.DayOfMonth!.Value, cadence.TimeOfDayUtc),
            _ => afterUtc.AddDays(1)
        };

        while (cadence.SkipDates.Contains(DateOnly.FromDateTime(candidate)))
            candidate = candidate.AddDays(1);

        return candidate;
    }

    private static DateTime NextWeekday(DateTime after, DayOfWeek dow, TimeOnly time)
    {
        var d = after.Date.AddDays(1);
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d.Add(time.ToTimeSpan());
    }

    private static DateTime NextMonthly(DateTime after, int day, TimeOnly time)
    {
        var next = new DateTime(after.Year, after.Month, Math.Min(day, DateTime.DaysInMonth(after.Year, after.Month)), time.Hour, time.Minute, 0, DateTimeKind.Utc);
        if (next <= after) next = next.AddMonths(1);
        return next;
    }

    private static DateTime NextQuarterly(DateTime after, int day, TimeOnly time)
    {
        var next = NextMonthly(after, day, time);
        while (!new[] { 1, 4, 7, 10 }.Contains(next.Month)) next = next.AddMonths(1);
        return next;
    }
}

public record ActivityClassificationResult(bool IsActivity, Enums.ActivityType? ActivityType, Enums.ActorRole? ActorRole);

public static class ActivityClassifier
{
    public static ActivityClassificationResult Classify(string eventType, string? actorRoleHint = null)
    {
        return eventType switch
        {
            "UserLoggedInIntegrationEvent" => new(true, ActivityType.Login, ActorRole.JobSeeker),
            "UserLoginFailedIntegrationEvent" => new(true, ActivityType.LoginFailed, ActorRole.System),
            "UserRegisteredIntegrationEvent" => new(true, ActivityType.UserRegistered, ActorRole.JobSeeker),
            "UserAccountActivatedIntegrationEvent" => new(true, ActivityType.AccountActivated, ActorRole.JobSeeker),
            "UserAccountSuspendedIntegrationEvent" => new(true, ActivityType.AccountSuspended, ActorRole.Administrator),
            "UserAccountReinstatedIntegrationEvent" => new(true, ActivityType.AccountReinstated, ActorRole.Administrator),
            "AccountDeactivatedIntegrationEvent" => new(true, ActivityType.AccountDeactivated, ActorRole.JobSeeker),
            "PasswordResetIntegrationEvent" => new(true, ActivityType.PasswordReset, ActorRole.JobSeeker),
            "RoleAssignedIntegrationEvent" => new(true, ActivityType.RoleAssigned, ActorRole.Administrator),
            "EmployerRegisteredIntegrationEvent" => new(true, ActivityType.EmployerRegistered, ActorRole.Employer),
            "EmployerProfileUpdatedIntegrationEvent" => new(true, ActivityType.EmployerProfileUpdated, ActorRole.Employer),
            "EmployerVerificationRequestedIntegrationEvent" => new(true, ActivityType.EmployerVerificationRequested, ActorRole.Employer),
            "EmployerVerifiedIntegrationEvent" => new(true, ActivityType.EmployerVerified, ActorRole.Administrator),
            "EmployerVerificationFailedIntegrationEvent" => new(true, ActivityType.EmployerVerificationFailed, ActorRole.Administrator),
            "CandidateSavedToTalentPoolIntegrationEvent" => new(true, ActivityType.CandidateSaved, ActorRole.Employer),
            "JobSeekerRegisteredIntegrationEvent" => new(true, ActivityType.JobSeekerRegistered, ActorRole.JobSeeker),
            "ProfileLevel2CompletedIntegrationEvent" => new(true, ActivityType.ProfileCompleted, ActorRole.JobSeeker),
            "ResumeUploadedIntegrationEvent" => new(true, ActivityType.ResumeUploaded, ActorRole.JobSeeker),
            "ResumeParsedIntegrationEvent" => new(true, ActivityType.ResumeParsed, ActorRole.JobSeeker),
            "ProfileSkillsUpdatedIntegrationEvent" => new(true, ActivityType.ProfileSkillsUpdated, ActorRole.JobSeeker),
            "ProfileVisibilityChangedIntegrationEvent" => new(true, ActivityType.ProfileVisibilityChanged, ActorRole.JobSeeker),
            "SupplementaryDocumentUploadedIntegrationEvent" => new(true, ActivityType.DocumentUploaded, ActorRole.JobSeeker),
            "ProfileCompletenessChangedIntegrationEvent" => new(true, ActivityType.ProfileCompletenessChanged, ActorRole.JobSeeker),
            "JobPostingCreatedIntegrationEvent" => new(true, ActivityType.JobPostingCreated, ActorRole.Employer),
            "JobPostingPublishedIntegrationEvent" => new(true, ActivityType.JobPostingPublished, ActorRole.Employer),
            "JobPostingUpdatedIntegrationEvent" => new(true, ActivityType.JobPostingUpdated, ActorRole.Employer),
            "JobPostingExpiredIntegrationEvent" => new(true, ActivityType.JobPostingExpired, ActorRole.System),
            "JobPostingClosedIntegrationEvent" => new(true, ActivityType.JobPostingClosed, ActorRole.Employer),
            "JobPostingSuspendedIntegrationEvent" => new(true, ActivityType.JobPostingSuspended, ActorRole.Administrator),
            "JobPostingReinstatedIntegrationEvent" => new(true, ActivityType.JobPostingReinstated, ActorRole.Administrator),
            "JobBookmarkedIntegrationEvent" => new(true, ActivityType.JobBookmark, ActorRole.JobSeeker),
            "JobUnbookmarkedIntegrationEvent" => new(true, ActivityType.JobUnbookmark, ActorRole.JobSeeker),
            "ApplicationSubmittedIntegrationEvent" => new(true, ActivityType.JobApplication, ActorRole.JobSeeker),
            "ApplicationViewedIntegrationEvent" => new(true, ActivityType.ApplicationReview, ActorRole.Employer),
            "ApplicationStatusChangedIntegrationEvent" => new(true, ActivityType.ApplicationStatusChanged, ActorRole.Employer),
            "ApplicationWithdrawnIntegrationEvent" => new(true, ActivityType.ApplicationWithdrawn, ActorRole.JobSeeker),
            "SearchPerformedIntegrationEvent" => actorRoleHint == "Employer"
                ? new(true, ActivityType.CandidateSearch, ActorRole.Employer)
                : new(true, ActivityType.JobSearch, ActorRole.JobSeeker),
            "SavedSearchCreatedIntegrationEvent" => new(true, ActivityType.SavedSearchCreated, ActorRole.JobSeeker),
            "SavedSearchMatchFoundIntegrationEvent" => new(true, ActivityType.SavedSearchMatch, ActorRole.System),
            "EmbeddingsRefreshedIntegrationEvent" => new(true, ActivityType.EmbeddingsRefreshed, ActorRole.System),
            "MatchThresholdChangedIntegrationEvent" => new(true, ActivityType.MatchThresholdChanged, ActorRole.Administrator),
            "ExternalJobIngestedIntegrationEvent" => new(true, ActivityType.ExternalJobIngested, ActorRole.System),
            "ExternalJobUpdatedIntegrationEvent" => new(true, ActivityType.ExternalJobUpdated, ActorRole.System),
            "ExternalJobRetractedIntegrationEvent" => new(true, ActivityType.ExternalJobRetracted, ActorRole.System),
            "IdentityVerifiedByGovernmentIntegrationEvent" => new(true, ActivityType.IdentityVerified, ActorRole.System),
            "IdentityVerificationFailedIntegrationEvent" => new(true, ActivityType.IdentityVerificationFailed, ActorRole.System),
            "EducationVerifiedIntegrationEvent" => new(true, ActivityType.EducationVerified, ActorRole.System),
            "EmployerVerifiedByGovernmentIntegrationEvent" => new(true, ActivityType.EmployerVerifiedByGovernment, ActorRole.System),
            "SyncErrorDetectedIntegrationEvent" => new(true, ActivityType.SyncError, ActorRole.System),
            "SyncReconciledIntegrationEvent" => new(true, ActivityType.SyncReconciled, ActorRole.System),
            "NotificationPreferencesUpdatedIntegrationEvent" => new(true, ActivityType.NotificationPreferencesUpdated, ActorRole.JobSeeker),
            "TaxonomyUpdatedIntegrationEvent" => new(true, ActivityType.TaxonomyUpdated, ActorRole.Administrator),
            "ArticlePublishedIntegrationEvent" => new(true, ActivityType.ArticlePublished, ActorRole.Administrator),
            "ArticleScheduledIntegrationEvent" => new(true, ActivityType.ArticleScheduled, ActorRole.Administrator),
            "ArticleArchivedIntegrationEvent" => new(true, ActivityType.ArticleArchived, ActorRole.Administrator),
            "FAQPublishedIntegrationEvent" => new(true, ActivityType.FAQPublished, ActorRole.Administrator),
            _ => new(false, null, null)
        };
    }
}
