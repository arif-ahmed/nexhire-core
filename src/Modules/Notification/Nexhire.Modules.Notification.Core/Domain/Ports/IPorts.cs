using System;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Notification.Core.Domain.Ports;

public interface IEmailChannel
{
    Task<Result<string>> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
}

public interface ISmsChannel
{
    Task<Result<string>> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default);
}

public interface IRealtimePush
{
    Task<Result> PushToastAsync(Guid userId, InAppToastDto toast, CancellationToken cancellationToken = default);
}

public interface IDncRegistry
{
    Task<bool> IsRegisteredAsync(string e164Number, CancellationToken cancellationToken = default);
}

public record EmailSendRequest(
    string ToAddress,
    string FromName,
    string FromAddress,
    string ReplyToAddress,
    string Subject,
    string BodyHtml,
    string BodyText,
    string ListUnsubscribeUrl,
    string SenderPostalAddress);

public record SmsSendRequest(
    string ToE164,
    string SenderId,
    string Body);

public record InAppToastDto(
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    string? ActionUrl,
    string Priority);
