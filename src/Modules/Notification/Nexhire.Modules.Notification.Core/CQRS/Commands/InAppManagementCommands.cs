using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Core.Domain;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;
using Nexhire.Modules.Notification.Core.Domain.Repositories;

namespace Nexhire.Modules.Notification.Core.CQRS.Commands;

public record MarkNotificationReadCommand(Guid UserId, NotificationId NotificationId) : ICommand;

public record MarkAllNotificationsReadCommand(Guid UserId) : ICommand;

public record ArchiveNotificationCommand(Guid UserId, NotificationId NotificationId) : ICommand<string>;

public record ArchiveNotificationsBatchCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

public record UndoArchiveNotificationCommand(Guid UserId, NotificationId NotificationId, string UndoToken) : ICommand;

public class InAppManagementCommandsHandler : 
    ICommandHandler<MarkNotificationReadCommand>,
    ICommandHandler<MarkAllNotificationsReadCommand>,
    ICommandHandler<ArchiveNotificationCommand, string>,
    ICommandHandler<ArchiveNotificationsBatchCommand>,
    ICommandHandler<UndoArchiveNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public InAppManagementCommandsHandler(
        INotificationRepository notificationRepository,
        INotificationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null || notification.RecipientUserId != request.UserId)
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));

        var result = notification.MarkRead();
        if (result.IsFailure) return result;

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        // Fetch up to 100 unread in-app notifications
        var unreads = await _notificationRepository.GetInAppForUserAsync(request.UserId, 0, 100, cancellationToken);
        foreach (var notification in unreads)
        {
            if (!notification.IsRead)
            {
                notification.MarkRead();
                _notificationRepository.Update(notification);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<string>> Handle(ArchiveNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null || notification.RecipientUserId != request.UserId)
            return Result.Failure<string>(new Error("Notification.NotFound", "Notification not found."));

        var result = notification.Archive();
        if (result.IsFailure) return Result.Failure<string>(result.Error);

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Simple cryptographically signed or base64 token valid for 10 seconds:
        string undoToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{notification.Id}:{DateTime.UtcNow:o}"));

        return undoToken;
    }

    public async Task<Result> Handle(ArchiveNotificationsBatchCommand request, CancellationToken cancellationToken)
    {
        if (request.NotificationIds == null || request.NotificationIds.Count > 50)
            return Result.Failure(new Error("E-NOTIF-BATCH-LIMIT", "Cannot archive more than 50 notifications in a single batch."));

        foreach (var idVal in request.NotificationIds)
        {
            var notif = await _notificationRepository.GetByIdAsync(new NotificationId(idVal), cancellationToken);
            if (notif != null && notif.RecipientUserId == request.UserId)
            {
                notif.Archive();
                _notificationRepository.Update(notif);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(UndoArchiveNotificationCommand request, CancellationToken cancellationToken)
    {
        // Verify token age:
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.UndoToken));
            var parts = decoded.Split(':');
            var timePart = string.Join(":", parts, 1, parts.Length - 1);
            var tokenTime = DateTime.Parse(timePart).ToUniversalTime();

            if (DateTime.UtcNow.Subtract(tokenTime).TotalSeconds > 10)
            {
                return Result.Failure(new Error("E-NOTIF-UNDO-EXPIRED", "The 10-second undo archive window has expired."));
            }
        }
        catch
        {
            return Result.Failure(new Error("Undo.InvalidToken", "Invalid undo token."));
        }

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null || notification.RecipientUserId != request.UserId)
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));

        var result = notification.Unarchive();
        if (result.IsFailure) return result;

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
