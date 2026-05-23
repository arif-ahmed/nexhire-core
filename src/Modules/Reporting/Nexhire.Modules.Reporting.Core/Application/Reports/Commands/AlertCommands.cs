using FluentValidation;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Core.Domain.Services;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Application.Reports.Commands;

public record CreateAlertRuleCommand(string Name, string MetricKey, Comparator Comparator, decimal Threshold, AlertSeverity Severity, List<AlertChannel> Channels, bool AnomalyDetectionEnabled, string CallerRole) : ICommand<Guid>;

public class CreateAlertRuleCommandValidator : AbstractValidator<CreateAlertRuleCommand>
{
    public CreateAlertRuleCommandValidator()
    {
        RuleFor(x => x.MetricKey).NotEmpty();
        RuleFor(x => x.Channels).NotEmpty();
        RuleFor(x => x.CallerRole).Equal("SystemAdministrator").WithMessage("E-REPORT-FORBIDDEN");
    }
}

public class CreateAlertRuleCommandHandler : ICommandHandler<CreateAlertRuleCommand, Guid>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public CreateAlertRuleCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result<Guid>> Handle(CreateAlertRuleCommand request, CancellationToken ct)
    {
        var condition = AlertCondition.Create(request.Comparator, request.Threshold).Value;
        var rule = AlertRule.Create(request.Name, request.MetricKey, condition, request.Severity, request.Channels, request.AnomalyDetectionEnabled);
        if (rule.IsFailure) return Result.Failure<Guid>(rule.Error);
        await _repo.AddAsync(rule.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(rule.Value.Id);
    }
}

public record EnableAlertRuleCommand(Guid RuleId) : ICommand;
public class EnableAlertRuleCommandHandler : ICommandHandler<EnableAlertRuleCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public EnableAlertRuleCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(EnableAlertRuleCommand request, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(request.RuleId, ct);
        if (r is null) return Result.Failure(new Error("AlertRule.NotFound", "Not found."));
        r.Enable(); _repo.Update(r); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record DisableAlertRuleCommand(Guid RuleId) : ICommand;
public class DisableAlertRuleCommandHandler : ICommandHandler<DisableAlertRuleCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public DisableAlertRuleCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(DisableAlertRuleCommand request, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(request.RuleId, ct);
        if (r is null) return Result.Failure(new Error("AlertRule.NotFound", "Not found."));
        r.Disable(); _repo.Update(r); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record AcknowledgeAlertIncidentCommand(Guid RuleId, Guid IncidentId, Guid ByUserId) : ICommand;
public class AcknowledgeAlertIncidentCommandHandler : ICommandHandler<AcknowledgeAlertIncidentCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public AcknowledgeAlertIncidentCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(AcknowledgeAlertIncidentCommand request, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(request.RuleId, ct);
        if (r is null) return Result.Failure(new Error("AlertRule.NotFound", "Not found."));
        var result = r.AcknowledgeIncident(request.IncidentId, request.ByUserId);
        if (result.IsFailure) return result;
        _repo.Update(r); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record SuppressAlertIncidentCommand(Guid RuleId, Guid IncidentId, DateTime UntilUtc) : ICommand;
public class SuppressAlertIncidentCommandHandler : ICommandHandler<SuppressAlertIncidentCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public SuppressAlertIncidentCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(SuppressAlertIncidentCommand request, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(request.RuleId, ct);
        if (r is null) return Result.Failure(new Error("AlertRule.NotFound", "Not found."));
        var result = r.SuppressIncident(request.IncidentId, request.UntilUtc);
        if (result.IsFailure) return result;
        _repo.Update(r); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record EscalateAlertIncidentCommand(Guid RuleId, Guid IncidentId) : ICommand;
public class EscalateAlertIncidentCommandHandler : ICommandHandler<EscalateAlertIncidentCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public EscalateAlertIncidentCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(EscalateAlertIncidentCommand request, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(request.RuleId, ct);
        if (r is null) return Result.Failure(new Error("AlertRule.NotFound", "Not found."));
        var result = r.EscalateIncident(request.IncidentId);
        if (result.IsFailure) return result;
        _repo.Update(r); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record EvaluateMetricForAlertsCommand(string MetricKey, decimal ObservedValue, DateTime MeasuredAt) : ICommand;
public class EvaluateMetricForAlertsCommandHandler : ICommandHandler<EvaluateMetricForAlertsCommand>
{
    private readonly IAlertRuleRepository _repo; private readonly IUnitOfWork _uow;
    public EvaluateMetricForAlertsCommandHandler(IAlertRuleRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(EvaluateMetricForAlertsCommand request, CancellationToken ct)
    {
        var rules = await _repo.GetEnabledByMetricKeyAsync(request.MetricKey, ct);
        foreach (var rule in rules)
        {
            if (rule.Condition.IsBreach(request.ObservedValue))
            {
                rule.Fire(request.ObservedValue, IncidentTrigger.ThresholdBreach, request.MeasuredAt);
                _repo.Update(rule);
            }
        }
        if (rules.Any()) await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
