using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;

namespace Nexhire.Modules.Notification.Core.Domain.Services;

public interface ITemplateRenderer
{
    Result<RenderedMessage> Render(TemplateVersion version, NotificationPayload payload, Channel channel);
}

public interface IChannelFanoutPlanner
{
    List<PlannedNotification> Plan(string eventType, string priorityString, RecipientPreferences prefs, DateTime nowUtc);
}

public interface IFrequencyCapEvaluator
{
    Result CheckSmsCap(Guid userId, int smsSentInLast24h, Priority priority, int capPerDay = 5);
}

public interface IDndScheduleCalculator
{
    DateTime? NextReleaseTimeUtc(DndWindow window, string ianaTimezone, DateTime nowUtc);
    Result<DateTime> CheckSmsSendWindow(string ianaTimezone, DateTime nowUtc);
}

public interface IDigestAssembler
{
    Result<RenderedMessage> Assemble(Digest digest, NotificationTemplate digestTemplate, List<NotificationPayload> itemPayloads);
}

public record PlannedNotification(Channel Channel, NotificationType Type, Priority Priority, Frequency Frequency, bool HeldForDnd);

public sealed class TemplateRenderer : ITemplateRenderer
{
    public Result<RenderedMessage> Render(TemplateVersion version, NotificationPayload payload, Channel channel)
    {
        if (version == null)
            return Result.Failure<RenderedMessage>(new Error("Renderer.VersionRequired", "Template version is required."));
        if (payload == null)
            return Result.Failure<RenderedMessage>(new Error("Renderer.PayloadRequired", "Payload is required."));

        string subject = version.Subject ?? "";
        string html = version.BodyHtml;
        string text = version.BodyText;
        string footer = version.Footer;

        // Perform simple template substitution for {{placeholder}}
        foreach (var placeholder in version.Placeholders)
        {
            string token = "{{" + placeholder + "}}";
            string val = "";
            if (payload.Values.TryGetValue(placeholder, out string? value))
            {
                val = value ?? "";
            }
            // Degrades gracefully: if missing, substitutes with empty string

            subject = subject.Replace(token, val);
            html = html.Replace(token, val);
            text = text.Replace(token, val);
            footer = footer.Replace(token, val);
        }

        // Handle simple conditional blocks: {{#if placeholder}}...{{/if}}
        foreach (var placeholder in version.Placeholders)
        {
            string ifStartToken = "{{#if " + placeholder + "}}";
            string ifEndToken = "{{/if}}";

            int startIdx;
            while ((startIdx = html.IndexOf(ifStartToken, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int endIdx = html.IndexOf(ifEndToken, startIdx, StringComparison.OrdinalIgnoreCase);
                if (endIdx == -1) break;

                bool hasValue = payload.Values.TryGetValue(placeholder, out string? value) && !string.IsNullOrWhiteSpace(value);
                
                string contentToKeep = "";
                if (hasValue)
                {
                    int contentStart = startIdx + ifStartToken.Length;
                    contentToKeep = html.Substring(contentStart, endIdx - contentStart);
                }

                html = html.Remove(startIdx, endIdx + ifEndToken.Length - startIdx);
                html = html.Insert(startIdx, contentToKeep);
            }

            while ((startIdx = text.IndexOf(ifStartToken, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int endIdx = text.IndexOf(ifEndToken, startIdx, StringComparison.OrdinalIgnoreCase);
                if (endIdx == -1) break;

                bool hasValue = payload.Values.TryGetValue(placeholder, out string? value) && !string.IsNullOrWhiteSpace(value);
                
                string contentToKeep = "";
                if (hasValue)
                {
                    int contentStart = startIdx + ifStartToken.Length;
                    contentToKeep = text.Substring(contentStart, endIdx - contentStart);
                }

                text = text.Remove(startIdx, endIdx + ifEndToken.Length - startIdx);
                text = text.Insert(startIdx, contentToKeep);
            }
        }

        // Apply footer if present
        if (!string.IsNullOrWhiteSpace(footer))
        {
            html += "<br/><footer style='font-size: 0.8em; color: #555;'>" + footer + "</footer>";
            text += "\n\n---\n" + footer;
        }

        bool isSms = channel == Channel.Sms;
        if (isSms)
        {
            // Strip any HTML from text version if accidentally populated, and render SMS body text only
            string plainTextOnly = RegexStripHtml(text);
            return RenderedMessage.Create(null, null, plainTextOnly, isSms: true);
        }

        return RenderedMessage.Create(subject, html, text, isSms: false);
    }

    private static string RegexStripHtml(string input)
    {
        return Regex.Replace(input, "<.*?>", string.Empty).Trim();
    }
}

public sealed class ChannelFanoutPlanner : IChannelFanoutPlanner
{
    private static readonly Dictionary<string, (NotificationType Type, Priority DefaultPriority)> EventTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "UserRegisteredIntegrationEvent", (NotificationType.Transactional, Priority.High) },
        { "UserAccountActivatedIntegrationEvent", (NotificationType.Transactional, Priority.Normal) },
        { "UserAccountSuspendedIntegrationEvent", (NotificationType.AccountSecurity, Priority.High) },
        { "UserAccountReinstatedIntegrationEvent", (NotificationType.AccountSecurity, Priority.Normal) },
        { "AccountDeactivatedIntegrationEvent", (NotificationType.Transactional, Priority.Normal) },
        { "UserLoginFailedIntegrationEvent", (NotificationType.AccountSecurity, Priority.High) },
        { "PasswordResetIntegrationEvent", (NotificationType.AccountSecurity, Priority.High) },
        { "OtpRequestedIntegrationEvent", (NotificationType.Transactional, Priority.High) },
        { "RoleAssignedIntegrationEvent", (NotificationType.AccountSecurity, Priority.Normal) },
        { "EmployerRegisteredIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "EmployerVerifiedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "EmployerVerificationFailedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.High) },
        { "CandidateSavedToTalentPoolIntegrationEvent", (NotificationType.RecruiterActivity, Priority.Normal) },
        { "JobPostingExpiredIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "JobPostingClosedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "JobPostingSuspendedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.High) },
        { "ApplicationSubmittedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "ApplicationStatusChangedIntegrationEvent", (NotificationType.ApplicationUpdate, Priority.Normal) },
        { "SavedSearchMatchFoundIntegrationEvent", (NotificationType.JobRecommendation, Priority.Normal) },
        { "RecommendationGeneratedIntegrationEvent", (NotificationType.JobRecommendation, Priority.Normal) },
        { "CandidateRecommendationGeneratedIntegrationEvent", (NotificationType.RecruiterActivity, Priority.Normal) },
        { "ProfileCompletenessChangedIntegrationEvent", (NotificationType.ProfileView, Priority.Normal) },
        { "SyncErrorDetectedIntegrationEvent", (NotificationType.Announcement, Priority.High) },
        { "ArticlePublishedIntegrationEvent", (NotificationType.Announcement, Priority.Normal) }
    };

    public List<PlannedNotification> Plan(string eventType, string priorityString, RecipientPreferences prefs, DateTime nowUtc)
    {
        var planned = new List<PlannedNotification>();
        if (prefs == null) return planned;

        if (!EventTypeMapping.TryGetValue(eventType, out var mapping))
        {
            // Unknown event types are logged and dropped per Conformist specification
            return planned;
        }

        Priority priority = mapping.DefaultPriority;
        if (!string.IsNullOrWhiteSpace(priorityString) && Enum.TryParse<Priority>(priorityString, true, out var parsedPriority))
        {
            priority = parsedPriority;
        }

        // Special override for ApplicationStatusChanged: if hired -> High priority
        if (eventType.Equals("ApplicationStatusChangedIntegrationEvent", StringComparison.OrdinalIgnoreCase) && priorityString.Equals("Hired", StringComparison.OrdinalIgnoreCase))
        {
            priority = Priority.High;
        }

        // Evaluate CanReceive for each of the three channels
        foreach (Channel channel in Enum.GetValues<Channel>())
        {
            var decision = prefs.CanReceive(channel, mapping.Type, priority, nowUtc);
            if (decision.Allowed)
            {
                planned.Add(new PlannedNotification(channel, mapping.Type, priority, decision.Frequency, decision.HeldForDnd));
            }
        }

        return planned;
    }
}

public sealed class FrequencyCapEvaluator : IFrequencyCapEvaluator
{
    public Result CheckSmsCap(Guid userId, int smsSentInLast24h, Priority priority, int capPerDay = 5)
    {
        if (priority == Priority.High)
        {
            return Result.Success(); // High priority security/verification SMS bypasses rolling caps
        }

        if (smsSentInLast24h >= capPerDay)
        {
            return Result.Failure(new Error("E-NOTIF-SMS-CAP-EXCEEDED", $"SMS frequency cap exceeded. Limit is {capPerDay} per rolling 24 hours."));
        }

        return Result.Success();
    }
}

public sealed class DndScheduleCalculator : IDndScheduleCalculator
{
    public DateTime? NextReleaseTimeUtc(DndWindow window, string ianaTimezone, DateTime nowUtc)
    {
        if (window == null) return null;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            var localTime = TimeOnly.FromDateTime(localNow);

            if (!window.IsInside(localTime)) return null;

            // Release time is at the end of the DND window local time
            var localRelease = localNow.Date.Add(window.EndLocalTime.ToTimeSpan());
            if (localRelease <= localNow)
            {
                localRelease = localRelease.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(localRelease, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback
            var localTime = TimeOnly.FromDateTime(nowUtc);
            if (!window.IsInside(localTime)) return null;

            var localRelease = nowUtc.Date.Add(window.EndLocalTime.ToTimeSpan());
            if (localRelease <= nowUtc)
            {
                localRelease = localRelease.AddDays(1);
            }
            return localRelease;
        }
    }

    public Result<DateTime> CheckSmsSendWindow(string ianaTimezone, DateTime nowUtc)
    {
        // Enforce TCPA quiet-hours: no SMS before 08:00 or after 21:00 in recipient local timezone
        var startSms = new TimeOnly(8, 0);
        var endSms = new TimeOnly(21, 0);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            var localTime = TimeOnly.FromDateTime(localNow);

            bool isLegal = localTime >= startSms && localTime <= endSms;
            if (isLegal)
            {
                return nowUtc;
            }

            // Outside window. Calculate next legal start time (08:00 local)
            var localNext = localNow.Date.Add(startSms.ToTimeSpan());
            if (localNext <= localNow)
            {
                localNext = localNext.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(localNext, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            var localTime = TimeOnly.FromDateTime(nowUtc);
            bool isLegal = localTime >= startSms && localTime <= endSms;
            if (isLegal)
            {
                return nowUtc;
            }

            var localNext = nowUtc.Date.Add(startSms.ToTimeSpan());
            if (localNext <= nowUtc)
            {
                localNext = localNext.AddDays(1);
            }
            return localNext;
        }
    }
}

public sealed class DigestAssembler : IDigestAssembler
{
    public Result<RenderedMessage> Assemble(Digest digest, NotificationTemplate digestTemplate, List<NotificationPayload> itemPayloads)
    {
        if (digest == null)
            return Result.Failure<RenderedMessage>(new Error("DigestAssembler.DigestRequired", "Digest is required."));
        if (digestTemplate == null)
            return Result.Failure<RenderedMessage>(new Error("DigestAssembler.TemplateRequired", "Digest template is required."));

        var version = digestTemplate.CurrentVersion;
        string itemsHtml = "";
        string itemsText = "";

        int count = digest.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = digest.Items.ElementAt(i);
            var payload = itemPayloads.ElementAtOrDefault(i);

            string title = item.Summary;
            if (payload != null && payload.Values.TryGetValue("job.title", out string? jobTitle))
            {
                title = jobTitle;
            }

            string action = item.ActionUrl ?? "#";

            itemsHtml += $"<div style='padding: 10px; border-bottom: 1px solid #eee;'><strong>{item.Type}</strong>: {title} <a href='{action}'>View</a></div>";
            itemsText += $"- [{item.Type}] {title} (Go to: {action})\n";
        }

        string html = version.BodyHtml
            .Replace("{{digest.count}}", count.ToString())
            .Replace("{{digest.items}}", itemsHtml);

        string text = version.BodyText
            .Replace("{{digest.count}}", count.ToString())
            .Replace("{{digest.items}}", itemsText);

        return RenderedMessage.Create(
            version.Subject?.Replace("{{digest.count}}", count.ToString()),
            html,
            text,
            isSms: false);
    }
}
