using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Notification.Core.Domain;

public sealed class EmailContactPoint : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Address { get; }
    public bool Verified { get; }
    public bool IsPreferred { get; }

    private EmailContactPoint(string address, bool verified, bool isPreferred)
    {
        Address = address;
        Verified = verified;
        IsPreferred = isPreferred;
    }

    public static Result<EmailContactPoint> Create(string address, bool verified = false, bool isPreferred = false)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Result.Failure<EmailContactPoint>(new Error("Email.Required", "Email address is required."));

        string trimmed = address.Trim().ToLowerInvariant();
        if (!EmailRegex.IsMatch(trimmed))
            return Result.Failure<EmailContactPoint>(new Error("Email.Invalid", "Invalid email address format."));

        return new EmailContactPoint(trimmed, verified, isPreferred);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
        yield return Verified;
        yield return IsPreferred;
    }
}

public sealed class PhoneContactPoint : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string E164Number { get; }
    public bool Verified { get; }
    public bool OptedIn { get; }

    private PhoneContactPoint(string e164Number, bool verified, bool optedIn)
    {
        E164Number = e164Number;
        Verified = verified;
        OptedIn = optedIn;
    }

    public static Result<PhoneContactPoint> Create(string rawNumber, bool verified = false, bool optedIn = false)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return Result.Failure<PhoneContactPoint>(new Error("Phone.Required", "Phone number is required."));

        string clean = rawNumber.Trim().Replace(" ", "").Replace("-", "");
        
        // Default region +880 (Bangladesh) handling if starts with 0 or 17 etc.
        if (clean.StartsWith("0"))
        {
            clean = "+88" + clean;
        }
        else if (!clean.StartsWith("+") && clean.Length == 11)
        {
            clean = "+88" + clean;
        }
        else if (!clean.StartsWith("+"))
        {
            clean = "+" + clean;
        }

        if (!PhoneRegex.IsMatch(clean))
            return Result.Failure<PhoneContactPoint>(new Error("Phone.Invalid", "Invalid E.164 phone number format."));

        return new PhoneContactPoint(clean, verified, optedIn);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return E164Number;
        yield return Verified;
        yield return OptedIn;
    }
}

public sealed class SourceEventRef : ValueObject
{
    public Guid EventId { get; }
    public string EventType { get; }
    public string SourceBc { get; }

    private SourceEventRef(Guid eventId, string eventType, string sourceBc)
    {
        EventId = eventId;
        EventType = eventType;
        SourceBc = sourceBc;
    }

    public static Result<SourceEventRef> Create(Guid eventId, string eventType, string sourceBc)
    {
        if (eventId == Guid.Empty)
            return Result.Failure<SourceEventRef>(new Error("SourceEvent.InvalidId", "Event ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<SourceEventRef>(new Error("SourceEvent.TypeRequired", "Event type is required."));
        if (string.IsNullOrWhiteSpace(sourceBc))
            return Result.Failure<SourceEventRef>(new Error("SourceEvent.BcRequired", "Source bounded context is required."));

        return new SourceEventRef(eventId, eventType.Trim(), sourceBc.Trim());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return EventId;
        yield return EventType;
        yield return SourceBc;
    }
}

public sealed class NotificationPayload : ValueObject
{
    public Dictionary<string, string> Values { get; }

    private NotificationPayload(Dictionary<string, string> values)
    {
        Values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }

    public static Result<NotificationPayload> Create(Dictionary<string, string> values)
    {
        if (values is null)
            return Result.Failure<NotificationPayload>(new Error("Payload.Required", "Payload values dictionary is required."));

        return new NotificationPayload(values);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var pair in Values)
        {
            yield return pair.Key;
            yield return pair.Value;
        }
    }
}

public sealed class RenderedMessage : ValueObject
{
    public string? Subject { get; }
    public string? BodyHtml { get; }
    public string BodyText { get; }
    public bool IsMultiSegmentSms { get; }

    private RenderedMessage(string? subject, string? bodyHtml, string bodyText, bool isMultiSegmentSms)
    {
        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        IsMultiSegmentSms = isMultiSegmentSms;
    }

    public static Result<RenderedMessage> Create(string? subject, string? bodyHtml, string bodyText, bool isSms = false)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
            return Result.Failure<RenderedMessage>(new Error("RenderedMessage.BodyRequired", "Body text is required."));

        bool isMultiSegment = false;
        if (isSms)
        {
            isMultiSegment = bodyText.Length > 160;
        }

        return new RenderedMessage(subject?.Trim(), bodyHtml?.Trim(), bodyText.Trim(), isMultiSegment);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Subject ?? string.Empty;
        yield return BodyHtml ?? string.Empty;
        yield return BodyText;
        yield return IsMultiSegmentSms;
    }
}

public sealed class EngagementState : ValueObject
{
    public DateTime? OpenedOnUtc { get; }
    public DateTime? ClickedOnUtc { get; }

    private EngagementState(DateTime? openedOnUtc, DateTime? clickedOnUtc)
    {
        OpenedOnUtc = openedOnUtc;
        ClickedOnUtc = clickedOnUtc;
    }

    public static Result<EngagementState> Create(DateTime? openedOnUtc = null, DateTime? clickedOnUtc = null)
    {
        if (openedOnUtc.HasValue && clickedOnUtc.HasValue && openedOnUtc.Value > clickedOnUtc.Value)
            return Result.Failure<EngagementState>(new Error("Engagement.InvalidChronology", "Opened date must be before or equal to Clicked date."));

        return new EngagementState(openedOnUtc, clickedOnUtc);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return OpenedOnUtc ?? DateTime.MinValue;
        yield return ClickedOnUtc ?? DateTime.MinValue;
    }
}

public sealed class DndWindow : ValueObject
{
    public TimeOnly StartLocalTime { get; }
    public TimeOnly EndLocalTime { get; }

    private DndWindow(TimeOnly startLocalTime, TimeOnly endLocalTime)
    {
        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
    }

    public static Result<DndWindow> Create(TimeOnly startLocalTime, TimeOnly endLocalTime)
    {
        return new DndWindow(startLocalTime, endLocalTime);
    }

    public bool IsInside(TimeOnly time)
    {
        if (StartLocalTime <= EndLocalTime)
        {
            return time >= StartLocalTime && time <= EndLocalTime;
        }
        else
        {
            // Wraps midnight (e.g. 22:00 to 07:00)
            return time >= StartLocalTime || time <= EndLocalTime;
        }
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartLocalTime;
        yield return EndLocalTime;
    }
}

public sealed class TemplateVersion : ValueObject
{
    public int VersionNumber { get; }
    public string? Subject { get; }
    public string BodyHtml { get; }
    public string BodyText { get; }
    public string Footer { get; }
    public List<string> Placeholders { get; }
    public DateTime CreatedOnUtc { get; }
    public Guid CreatedByUserId { get; }

    private TemplateVersion(
        int versionNumber,
        string? subject,
        string bodyHtml,
        string bodyText,
        string footer,
        List<string> placeholders,
        DateTime createdOnUtc,
        Guid createdByUserId)
    {
        VersionNumber = versionNumber;
        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        Footer = footer;
        Placeholders = placeholders;
        CreatedOnUtc = createdOnUtc;
        CreatedByUserId = createdByUserId;
    }

    public static Result<TemplateVersion> Create(
        int versionNumber,
        string? subject,
        string bodyHtml,
        string bodyText,
        string footer,
        List<string> placeholders,
        DateTime createdOnUtc,
        Guid createdByUserId)
    {
        if (versionNumber <= 0)
            return Result.Failure<TemplateVersion>(new Error("Template.VersionInvalid", "Version number must be strictly positive."));
        if (string.IsNullOrWhiteSpace(bodyHtml))
            return Result.Failure<TemplateVersion>(new Error("Template.BodyHtmlRequired", "Body HTML is required."));
        if (string.IsNullOrWhiteSpace(bodyText))
            return Result.Failure<TemplateVersion>(new Error("Template.BodyTextRequired", "Body text is required."));

        // Size check (BodyHtml <= 100 KB and <= 50 000 chars)
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(bodyHtml);
        if (bytes.Length > 102400 || bodyHtml.Length > 50000)
            return Result.Failure<TemplateVersion>(new Error("E-NOTIF-TEMPLATE-TOO-LARGE", "Body HTML is too large. Must be <= 100KB and <= 50,000 characters."));

        return new TemplateVersion(
            versionNumber,
            subject?.Trim(),
            bodyHtml,
            bodyText,
            footer.Trim(),
            placeholders ?? new List<string>(),
            createdOnUtc,
            createdByUserId);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return VersionNumber;
        yield return Subject ?? string.Empty;
        yield return BodyHtml;
        yield return BodyText;
        yield return Footer;
        yield return CreatedOnUtc;
        yield return CreatedByUserId;
    }
}
