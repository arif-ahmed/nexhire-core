using FluentValidation;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Application.Reports.Commands;

public record CreateReportScheduleCommand(
    Guid DefinitionId, Guid OwnerUserId, string CallerRole,
    Frequency Frequency, DayOfWeek? DayOfWeek, int? DayOfMonth,
    TimeOnly TimeOfDayUtc, List<string> DistributionEmails,
    List<ExportFormat> ExportFormats) : ICommand<Guid>;

public class CreateReportScheduleCommandValidator : AbstractValidator<CreateReportScheduleCommand>
{
    public CreateReportScheduleCommandValidator()
    {
        RuleFor(x => x.DefinitionId).NotEmpty();
        RuleFor(x => x.DistributionEmails).NotEmpty();
        RuleFor(x => x.ExportFormats).NotEmpty();
        RuleFor(x => x.CallerRole).Must(r => new[] { "SystemAdministrator", "MoLAdministrator" }.Contains(r))
            .WithMessage("E-REPORT-FORBIDDEN");
    }
}

public class CreateReportScheduleCommandHandler : ICommandHandler<CreateReportScheduleCommand, Guid>
{
    private readonly IReportScheduleRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateReportScheduleCommandHandler(IReportScheduleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result<Guid>> Handle(CreateReportScheduleCommand request, CancellationToken ct)
    {
        var cadenceResult = ScheduleCadence.Create(request.Frequency, request.DayOfWeek, request.DayOfMonth, request.TimeOfDayUtc, new List<DateOnly>());
        if (cadenceResult.IsFailure) return Result.Failure<Guid>(cadenceResult.Error);

        var emails = new List<EmailAddress>();
        foreach (var e in request.DistributionEmails)
        {
            var emailResult = EmailAddress.Create(e);
            if (emailResult.IsFailure) return Result.Failure<Guid>(emailResult.Error);
            emails.Add(emailResult.Value);
        }

        var schedule = ReportSchedule.Create(request.DefinitionId, cadenceResult.Value,
            ResolvedParameters.Empty(), emails, request.ExportFormats, request.OwnerUserId);
        if (schedule.IsFailure) return Result.Failure<Guid>(schedule.Error);

        await _repo.AddAsync(schedule.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(schedule.Value.Id);
    }
}

public record PauseReportScheduleCommand(Guid ScheduleId) : ICommand;
public class PauseReportScheduleCommandHandler : ICommandHandler<PauseReportScheduleCommand>
{
    private readonly IReportScheduleRepository _repo; private readonly IUnitOfWork _uow;
    public PauseReportScheduleCommandHandler(IReportScheduleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(PauseReportScheduleCommand request, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(request.ScheduleId, ct);
        if (s is null) return Result.Failure(new Error("ReportSchedule.NotFound", "Not found."));
        var r = s.Pause(); if (r.IsFailure) return r;
        _repo.Update(s); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record ResumeReportScheduleCommand(Guid ScheduleId) : ICommand;
public class ResumeReportScheduleCommandHandler : ICommandHandler<ResumeReportScheduleCommand>
{
    private readonly IReportScheduleRepository _repo; private readonly IUnitOfWork _uow;
    public ResumeReportScheduleCommandHandler(IReportScheduleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(ResumeReportScheduleCommand request, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(request.ScheduleId, ct);
        if (s is null) return Result.Failure(new Error("ReportSchedule.NotFound", "Not found."));
        var r = s.Resume(); if (r.IsFailure) return r;
        _repo.Update(s); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record DeleteReportScheduleCommand(Guid ScheduleId) : ICommand;
public class DeleteReportScheduleCommandHandler : ICommandHandler<DeleteReportScheduleCommand>
{
    private readonly IReportScheduleRepository _repo; private readonly IUnitOfWork _uow;
    public DeleteReportScheduleCommandHandler(IReportScheduleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(DeleteReportScheduleCommand request, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(request.ScheduleId, ct);
        if (s is null) return Result.Failure(new Error("ReportSchedule.NotFound", "Not found."));
        _repo.Remove(s); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}
