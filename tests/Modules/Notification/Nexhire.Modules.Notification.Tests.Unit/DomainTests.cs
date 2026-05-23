using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using Nexhire.Modules.Notification.Core.Domain;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;
using NotificationAggregate = Nexhire.Modules.Notification.Core.Domain.Aggregates.Notification;
using Nexhire.Modules.Notification.Core.Domain.Events;
using Nexhire.Modules.Notification.Core.Domain.Services;
using Nexhire.Modules.Notification.Core.Domain;

namespace Nexhire.Modules.Notification.Tests.Unit;

public class DomainTests
{
    [Fact]
    public void EmailContactPoint_Create_Should_LowerAndTrimValue_WhenValid()
    {
        // Act
        var result = EmailContactPoint.Create("  Test@NexHire.CoM  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be("test@nexhire.com");
    }

    [Fact]
    public void EmailContactPoint_Create_Should_ReturnFailure_WhenFormatInvalid()
    {
        // Act
        var result = EmailContactPoint.Create("invalid-email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }

    [Fact]
    public void PhoneContactPoint_Create_Should_FormatWithBangladeshPrefix_WhenStartsWithoutCountryCode()
    {
        // Act
        var result = PhoneContactPoint.Create("01711122233");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.E164Number.Should().Be("+8801711122233");
    }

    [Fact]
    public void PhoneContactPoint_Create_Should_FormatWithPrefix_WhenInternationalFormat()
    {
        // Act
        var result = PhoneContactPoint.Create("+8801811122233");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.E164Number.Should().Be("+8801811122233");
    }

    [Fact]
    public void TemplateVersion_Create_Should_Fail_WhenBodyHtmlExceedsSizeLimit()
    {
        // Arrange - Create an oversized HTML body string (> 100 KB)
        string largeHtml = new('a', 105000);

        // Act
        var result = TemplateVersion.Create(
            1,
            "Subject",
            largeHtml,
            "Plain Text",
            "Footer",
            new List<string>(),
            DateTime.UtcNow,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-NOTIF-TEMPLATE-TOO-LARGE");
    }

    [Fact]
    public void Notification_Create_Should_StartInPending_AndRaiseDomainEvent()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var source = SourceEventRef.Create(Guid.NewGuid(), "UserRegisteredIntegrationEvent", "IAM").Value;
        var payload = NotificationPayload.Create(new Dictionary<string, string>()).Value;

        // Act
        var result = NotificationAggregate.Create(
            recipientId,
            Channel.Email,
            NotificationType.Transactional,
            Priority.High,
            source,
            payload,
            DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var notif = result.Value;
        notif.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        notif.IsRead.Should().BeFalse();
        notif.DomainEvents.Should().ContainSingle(e => e is NotificationCreated);
    }

    [Fact]
    public void Notification_MarkRead_Should_Fail_WhenChannelIsNotInApp()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var source = SourceEventRef.Create(Guid.NewGuid(), "UserRegisteredIntegrationEvent", "IAM").Value;
        var payload = NotificationPayload.Create(new Dictionary<string, string>()).Value;
        var notif = NotificationAggregate.Create(recipientId, Channel.Email, NotificationType.Transactional, Priority.High, source, payload, DateTime.UtcNow).Value;

        // Act
        var result = notif.MarkRead();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-NOTIF-WRONG-CHANNEL");
    }

    [Fact]
    public void Notification_RecordSendAttempt_Should_ChangeStatusToSent_AndLogAttempt()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var source = SourceEventRef.Create(Guid.NewGuid(), "UserRegisteredIntegrationEvent", "IAM").Value;
        var payload = NotificationPayload.Create(new Dictionary<string, string>()).Value;
        var notif = NotificationAggregate.Create(recipientId, Channel.Email, NotificationType.Transactional, Priority.High, source, payload, DateTime.UtcNow).Value;
        
        var templateVer = TemplateVersion.Create(1, "Sub", "Html", "Text", "Foot", new(), DateTime.UtcNow, Guid.NewGuid()).Value;
        var rendered = RenderedMessage.Create("Sub", "Html", "Text").Value;
        notif.Render(templateVer, rendered);

        // Act
        var result = notif.RecordSendAttempt("prov_msg_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        notif.DeliveryStatus.Should().Be(DeliveryStatus.Sent);
        notif.Attempts.Should().HaveCount(1);
        notif.Attempts.First().Outcome.Should().Be(AttemptOutcome.Succeeded);
    }

    [Fact]
    public void Notification_RecordSoftBounce_Should_FlipToFailed_OnThirdAttempt()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var source = SourceEventRef.Create(Guid.NewGuid(), "UserRegisteredIntegrationEvent", "IAM").Value;
        var payload = NotificationPayload.Create(new Dictionary<string, string>()).Value;
        var notif = NotificationAggregate.Create(recipientId, Channel.Email, NotificationType.Transactional, Priority.High, source, payload, DateTime.UtcNow).Value;

        // Act
        notif.RecordSoftBounce("Soft bounce 1");
        notif.RecordSoftBounce("Soft bounce 2");
        notif.RecordSoftBounce("Soft bounce 3");

        // Assert
        notif.DeliveryStatus.Should().Be(DeliveryStatus.Failed);
        notif.Attempts.Should().HaveCount(3);
    }

    [Fact]
    public void RecipientPreferences_CanReceive_Should_AllowCriticalNotifications_EvenWhenSuppressed()
    {
        // Arrange
        var email = EmailContactPoint.Create("admin@nexhire.com").Value;
        var prefs = RecipientPreferences.CreateDefault(Guid.NewGuid(), "Admin", email, DateTime.UtcNow).Value;
        
        // Suppress email globally
        prefs.SetGlobalEmailOptOut(true, "SettingsTest", null);
        prefs.SuppressEmail("TestBounce");

        // Act - High priority transactional security notification
        var decision = prefs.CanReceive(Channel.Email, NotificationType.AccountSecurity, Priority.High, DateTime.UtcNow);

        // Assert
        decision.Allowed.Should().BeTrue();
        decision.Frequency.Should().Be(Frequency.Immediate);
    }

    [Fact]
    public void RecipientPreferences_CanReceive_Should_HoldNotification_DuringDndQuietHours()
    {
        // Arrange
        var email = EmailContactPoint.Create("user@nexhire.com").Value;
        var prefs = RecipientPreferences.CreateDefault(Guid.NewGuid(), "Candidate", email, DateTime.UtcNow).Value;
        
        // DND window between 22:00 and 08:00
        var window = DndWindow.Create(new TimeOnly(22, 0), new TimeOnly(8, 0)).Value;
        prefs.SetDoNotDisturb(window);
        prefs.SetTimezone("Asia/Dhaka");

        // Act - Normal priority update during DND (simulated local time 23:00)
        // DateTime.UtcNow at 17:00 UTC is 23:00 local time in Asia/Dhaka (+6)
        var utcNowDuringDnd = new DateTime(2026, 5, 23, 17, 0, 0, DateTimeKind.Utc);
        var decision = prefs.CanReceive(Channel.Email, NotificationType.JobRecommendation, Priority.Normal, utcNowDuringDnd);

        // Assert
        decision.Allowed.Should().BeTrue();
        decision.HeldForDnd.Should().BeTrue();
    }

    [Fact]
    public void RecipientPreferences_OptInSms_Should_Fail_WhenPhoneIsUnverified()
    {
        // Arrange
        var email = EmailContactPoint.Create("user@nexhire.com").Value;
        var prefs = RecipientPreferences.CreateDefault(Guid.NewGuid(), "Candidate", email, DateTime.UtcNow).Value;
        
        // Add phone contact without verifying
        var phone = PhoneContactPoint.Create("01711223344", verified: false).Value;
        prefs.ProvidePhoneNumber(phone);

        // Act
        var result = prefs.OptInSms("SettingsToggle", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-NOTIF-PHONE-UNVERIFIED");
    }

    [Fact]
    public void TemplateRenderer_Render_Should_SubstitutePlaceholders_Gracefully()
    {
        // Arrange
        var renderer = new TemplateRenderer();
        var version = TemplateVersion.Create(
            1,
            "Welcome {{user.name}}!",
            "<p>Hello {{user.name}}, your application for {{job.title}} is verified.</p>",
            "Hello {{user.name}}, your application for {{job.title}} is verified.",
            "Best, Team",
            new List<string> { "user.name", "job.title" },
            DateTime.UtcNow,
            Guid.NewGuid()).Value;

        var payload = NotificationPayload.Create(new Dictionary<string, string>
        {
            { "user.name", "Arif Ahmed" }
            // "job.title" placeholder is intentionally missing to test graceful degradation
        }).Value;

        // Act
        var renderResult = renderer.Render(version, payload, Channel.Email);

        // Assert
        renderResult.IsSuccess.Should().BeTrue();
        renderResult.Value.Subject.Should().Be("Welcome Arif Ahmed!");
        renderResult.Value.BodyText.Should().Contain("Hello Arif Ahmed, your application for  is verified.");
    }
}
