using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Domain.Events;

namespace Nexhire.Modules.Notification.Domain.Aggregates;

public sealed class ChannelDecision : ValueObject
{
    public bool Allowed { get; }
    public Frequency Frequency { get; }
    public bool HeldForDnd { get; }
    public string? Reason { get; }

    private ChannelDecision(bool allowed, Frequency frequency, bool heldForDnd, string? reason)
    {
        Allowed = allowed;
        Frequency = frequency;
        HeldForDnd = heldForDnd;
        Reason = reason;
    }

    public static ChannelDecision Allow(Frequency frequency, bool heldForDnd = false) =>
        new(true, frequency, heldForDnd, null);

    public static ChannelDecision Deny(string reason) =>
        new(false, Frequency.Immediate, false, reason);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Allowed;
        yield return Frequency;
        yield return HeldForDnd;
        yield return Reason ?? string.Empty;
    }
}

public sealed class ChannelTypePreference : Entity<Guid>
{
    public Channel Channel { get; private set; }
    public NotificationType Type { get; private set; }
    public bool Enabled { get; private set; }
    public Frequency Frequency { get; private set; }
    public ToastMode ToastMode { get; private set; }

    private ChannelTypePreference() { } // EF Core

    internal ChannelTypePreference(
        Guid id,
        Channel channel,
        NotificationType type,
        bool enabled,
        Frequency frequency,
        ToastMode toastMode) : base(id)
    {
        Channel = channel;
        Type = type;
        Enabled = enabled;
        Frequency = frequency;
        ToastMode = toastMode;
    }

    internal void Update(bool enabled, Frequency frequency, ToastMode toastMode)
    {
        Enabled = enabled;
        Frequency = frequency;
        ToastMode = toastMode;
    }
}

public sealed class ConsentRecord : Entity<Guid>
{
    public Channel Channel { get; private set; }
    public string Decision { get; private set; } = null!; // OptIn or OptOut
    public string Method { get; private set; } = null!;
    public string? IpAddress { get; private set; }
    public DateTime RecordedOnUtc { get; private set; }

    private ConsentRecord() { } // EF Core

    internal ConsentRecord(
        Guid id,
        Channel channel,
        string decision,
        string method,
        string? ipAddress,
        DateTime recordedOnUtc) : base(id)
    {
        Channel = channel;
        Decision = decision;
        Method = method;
        IpAddress = ipAddress;
        RecordedOnUtc = recordedOnUtc;
    }
}

public sealed class RecipientPreferences : AggregateRoot<RecipientPreferencesId>
{
    private readonly List<ChannelTypePreference> _channelTypePrefs = new();
    private readonly List<ConsentRecord> _consents = new();

    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public EmailContactPoint EmailContact { get; private set; } = null!;
    public PhoneContactPoint? SmsContact { get; private set; }
    public string Timezone { get; private set; } = "Asia/Dhaka";
    public DndWindow? DoNotDisturb { get; private set; }
    public bool GlobalEmailOptOut { get; private set; }
    public bool SmsSuppressed { get; private set; }
    public bool EmailSuppressed { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public int VersionToken { get; private set; }

    public IReadOnlyCollection<ChannelTypePreference> ChannelTypePrefs => _channelTypePrefs.AsReadOnly();
    public IReadOnlyCollection<ConsentRecord> Consents => _consents.AsReadOnly();

    private RecipientPreferences() { } // EF Core

    private RecipientPreferences(
        RecipientPreferencesId id,
        Guid userId,
        string role,
        EmailContactPoint emailContact,
        DateTime nowUtc) : base(id)
    {
        UserId = userId;
        Role = role;
        EmailContact = emailContact;
        GlobalEmailOptOut = false;
        SmsSuppressed = false;
        EmailSuppressed = false;
        CreatedOnUtc = nowUtc;
        UpdatedOnUtc = nowUtc;
        VersionToken = 1;
    }

    public static Result<RecipientPreferences> CreateDefault(
        Guid userId,
        string role,
        EmailContactPoint email,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
            return Result.Failure<RecipientPreferences>(new Error("Preferences.UserRequired", "User ID is required."));
        if (email is null)
            return Result.Failure<RecipientPreferences>(new Error("Preferences.EmailRequired", "Email contact point is required."));

        var id = new RecipientPreferencesId(Guid.NewGuid());
        var prefs = new RecipientPreferences(id, userId, role, email, nowUtc);

        // Prepopulate default toggles (enabled, immediate) for all combinations
        foreach (Channel channel in Enum.GetValues<Channel>())
        {
            foreach (NotificationType type in Enum.GetValues<NotificationType>())
            {
                // SMS defaults to disabled since it requires explicit verification/opt-in first
                bool enabled = channel != Channel.Sms;
                
                prefs._channelTypePrefs.Add(new ChannelTypePreference(
                    Guid.NewGuid(),
                    channel,
                    type,
                    enabled,
                    Frequency.Immediate,
                    ToastMode.Toast));
            }
        }

        // Add initial consent for email
        prefs._consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Email,
            "OptIn",
            "RegistrationCheckbox",
            null,
            nowUtc));

        return prefs;
    }

    public Result SetChannelTypePreference(Channel channel, NotificationType type, bool enabled, Frequency frequency, ToastMode toastMode = ToastMode.Toast)
    {
        if (channel == Channel.Sms && frequency != Frequency.Immediate)
            return Result.Failure(new Error("Preferences.SmsCannotDigest", "SMS preferences do not support digest frequency."));

        // Critical email categories (AccountSecurity/Transactional) cannot be disabled
        if (channel == Channel.Email && (type == NotificationType.AccountSecurity || type == NotificationType.Transactional) && !enabled)
            return Result.Failure(new Error("E-NOTIF-CRITICAL-LOCKED", "Critical email notifications (AccountSecurity and Transactional) cannot be disabled."));

        var cell = _channelTypePrefs.FirstOrDefault(p => p.Channel == channel && p.Type == type);
        if (cell != null)
        {
            cell.Update(enabled, frequency, toastMode);
        }
        else
        {
            _channelTypePrefs.Add(new ChannelTypePreference(Guid.NewGuid(), channel, type, enabled, frequency, toastMode));
        }

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
        return Result.Success();
    }

    public Result SetPreferredEmail(EmailContactPoint email)
    {
        if (email is null)
            return Result.Failure(new Error("Preferences.EmailRequired", "Preferred email contact is required."));
        if (!email.Verified)
            return Result.Failure(new Error("Preferences.EmailUnverified", "Preferred email must be verified."));

        EmailContact = email;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
        return Result.Success();
    }

    public void AddOrReplaceEmailContact(EmailContactPoint email)
    {
        if (email != null)
        {
            if (EmailContact == null || EmailContact.Address != email.Address)
            {
                EmailSuppressed = false; // reset suppression if email changed
            }
            EmailContact = email;
            UpdatedOnUtc = DateTime.UtcNow;
        }
    }

    public void ProvidePhoneNumber(PhoneContactPoint phone)
    {
        SmsContact = phone;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result ConfirmPhoneNumber()
    {
        if (SmsContact == null)
            return Result.Failure(new Error("Preferences.NoPhoneOnFile", "No phone number is registered."));

        var confirmedPhone = PhoneContactPoint.Create(SmsContact.E164Number, verified: true, optedIn: SmsContact.OptedIn).Value;
        SmsContact = confirmedPhone;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result OptInSms(string method, string? ipAddress)
    {
        if (SmsContact == null)
            return Result.Failure(new Error("Preferences.NoPhoneOnFile", "No phone number is registered."));
        if (!SmsContact.Verified)
            return Result.Failure(new Error("E-NOTIF-PHONE-UNVERIFIED", "Phone number must be verified before opting in to SMS."));

        SmsContact = PhoneContactPoint.Create(SmsContact.E164Number, verified: true, optedIn: true).Value;
        SmsSuppressed = false;

        _consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Sms,
            "OptIn",
            method,
            ipAddress,
            DateTime.UtcNow));

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
        return Result.Success();
    }

    public Result OptOutSms(string method, string? ipAddress)
    {
        if (SmsContact == null)
            return Result.Failure(new Error("Preferences.NoPhoneOnFile", "No phone number is registered."));

        SmsContact = PhoneContactPoint.Create(SmsContact.E164Number, verified: SmsContact.Verified, optedIn: false).Value;

        _consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Sms,
            "OptOut",
            method,
            ipAddress,
            DateTime.UtcNow));

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
        return Result.Success();
    }

    public Result SetGlobalEmailOptOut(bool optedOut, string method, string? ipAddress)
    {
        GlobalEmailOptOut = optedOut;
        _consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Email,
            optedOut ? "OptOut" : "OptIn",
            method,
            ipAddress,
            DateTime.UtcNow));

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
        return Result.Success();
    }

    public void SetDoNotDisturb(DndWindow? window)
    {
        DoNotDisturb = window;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
    }

    public Result SetTimezone(string ianaTz)
    {
        if (string.IsNullOrWhiteSpace(ianaTz))
            return Result.Failure(new Error("Preferences.TimezoneRequired", "Timezone is required."));

        Timezone = ianaTz.Trim();
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void SuppressEmail(string reason)
    {
        EmailSuppressed = true;
        _consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Email,
            "OptOut",
            "Suppression:" + reason,
            null,
            DateTime.UtcNow));
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void SuppressSms(string reason)
    {
        SmsSuppressed = true;
        if (SmsContact != null)
        {
            SmsContact = PhoneContactPoint.Create(SmsContact.E164Number, SmsContact.Verified, optedIn: false).Value;
        }
        _consents.Add(new ConsentRecord(
            Guid.NewGuid(),
            Channel.Sms,
            "OptOut",
            "Suppression:" + reason,
            null,
            DateTime.UtcNow));
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationPreferencesUpdated(Guid.NewGuid(), UserId, DateTime.UtcNow));
    }

    public ChannelDecision CanReceive(Channel channel, NotificationType type, Priority priority, DateTime nowUtc)
    {
        // 1. Critical security and transactional bypasses everything.
        if (priority == Priority.High && (type == NotificationType.AccountSecurity || type == NotificationType.Transactional))
        {
            return ChannelDecision.Allow(Frequency.Immediate);
        }

        // 2. Email Suppression or Global Opt-Out
        if (channel == Channel.Email)
        {
            if ((GlobalEmailOptOut || EmailSuppressed) && type != NotificationType.Transactional && type != NotificationType.AccountSecurity)
            {
                return ChannelDecision.Deny("email-suppressed");
            }
        }

        // 3. SMS Opt-In / Verification Check
        if (channel == Channel.Sms)
        {
            if (SmsContact == null || !SmsContact.Verified || !SmsContact.OptedIn || SmsSuppressed)
            {
                return ChannelDecision.Deny("sms-not-opted-in");
            }

            // 4. SMS Critical-Only rule
            if (type != NotificationType.AccountSecurity && type != NotificationType.Transactional)
            {
                return ChannelDecision.Deny("sms-non-critical");
            }
        }

        // 5. Look up the specific cell toggle
        var cell = _channelTypePrefs.FirstOrDefault(p => p.Channel == channel && p.Type == type);
        if (cell == null || !cell.Enabled)
        {
            return ChannelDecision.Deny("type-disabled");
        }

        // 6. In-App CenterOnly Check
        if (channel == Channel.InApp && cell.ToastMode == ToastMode.Disabled)
        {
            return ChannelDecision.Deny("type-disabled");
        }

        // 7. Do Not Disturb Local Time check
        if (DoNotDisturb != null && priority == Priority.Normal)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(Timezone);
                var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz));
                if (DoNotDisturb.IsInside(localTime))
                {
                    return ChannelDecision.Allow(cell.Frequency, heldForDnd: true);
                }
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: system timezone
                var localTime = TimeOnly.FromDateTime(nowUtc);
                if (DoNotDisturb.IsInside(localTime))
                {
                    return ChannelDecision.Allow(cell.Frequency, heldForDnd: true);
                }
            }
        }

        return ChannelDecision.Allow(cell.Frequency);
    }
}
