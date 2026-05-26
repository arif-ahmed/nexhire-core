using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Domain;
using Nexhire.Modules.Notification.Domain.Aggregates;
using Nexhire.Modules.Notification.Domain.Repositories;

namespace Nexhire.Modules.Notification.Application.CQRS.Commands;

public record CreateTemplateCommand(
    string ChannelString,
    string TypeString,
    string Name,
    string? Subject,
    string BodyHtml,
    string BodyText,
    string Footer,
    List<string> Placeholders,
    Guid CreatedByUserId) : ICommand<Guid>;

public record PublishTemplateVersionCommand(
    Guid TemplateId,
    string? Subject,
    string BodyHtml,
    string BodyText,
    string Footer,
    List<string> Placeholders,
    Guid CreatedByUserId) : ICommand<int>;

public record RollbackTemplateCommand(
    Guid TemplateId,
    int VersionNumber,
    Guid CreatedByUserId) : ICommand;

public class TemplateManagementCommandsHandler :
    ICommandHandler<CreateTemplateCommand, Guid>,
    ICommandHandler<PublishTemplateVersionCommand, int>,
    ICommandHandler<RollbackTemplateCommand>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public TemplateManagementCommandsHandler(
        INotificationTemplateRepository templateRepository,
        INotificationUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Channel>(request.ChannelString, true, out var channel))
            return Result.Failure<Guid>(new Error("Template.InvalidChannel", "Invalid channel specified."));
        if (!Enum.TryParse<NotificationType>(request.TypeString, true, out var type))
            return Result.Failure<Guid>(new Error("Template.InvalidType", "Invalid notification type specified."));

        var existing = await _templateRepository.GetByChannelAndTypeAsync(channel, type, cancellationToken);
        if (existing != null)
            return Result.Failure<Guid>(new Error("Template.Duplicate", $"A template already exists for {channel} and {type}."));

        var versionResult = TemplateVersion.Create(
            1,
            request.Subject,
            request.BodyHtml,
            request.BodyText,
            request.Footer,
            request.Placeholders,
            DateTime.UtcNow,
            request.CreatedByUserId);

        if (versionResult.IsFailure) return Result.Failure<Guid>(versionResult.Error);

        var templateResult = NotificationTemplate.Create(
            channel,
            type,
            request.Name,
            versionResult.Value,
            DateTime.UtcNow);

        if (templateResult.IsFailure) return Result.Failure<Guid>(templateResult.Error);

        await _templateRepository.AddAsync(templateResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return templateResult.Value.Id.Value;
    }

    public async Task<Result<int>> Handle(PublishTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(new NotificationTemplateId(request.TemplateId), cancellationToken);
        if (template == null)
            return Result.Failure<int>(new Error("Template.NotFound", "Template not found."));

        int nextVerNo = template.CurrentVersion.VersionNumber + 1;
        var versionResult = TemplateVersion.Create(
            nextVerNo,
            request.Subject,
            request.BodyHtml,
            request.BodyText,
            request.Footer,
            request.Placeholders,
            DateTime.UtcNow,
            request.CreatedByUserId);

        if (versionResult.IsFailure) return Result.Failure<int>(versionResult.Error);

        var pubResult = template.PublishNewVersion(versionResult.Value, request.CreatedByUserId);
        if (pubResult.IsFailure) return Result.Failure<int>(pubResult.Error);

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return nextVerNo;
    }

    public async Task<Result> Handle(RollbackTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(new NotificationTemplateId(request.TemplateId), cancellationToken);
        if (template == null)
            return Result.Failure(new Error("Template.NotFound", "Template not found."));

        var rollbackResult = template.RollbackTo(request.VersionNumber, request.CreatedByUserId);
        if (rollbackResult.IsFailure) return rollbackResult;

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
