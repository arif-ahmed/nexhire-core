using FluentValidation;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Ports;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Core.Domain.Services;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Application.Reports.Commands;

public record CreateRetentionPolicyCommand(string Name, ActorRole ActorRole, int RetentionDays, RetentionAction Action, int WarningDays, string CallerRole) : ICommand<Guid>;

public class CreateRetentionPolicyCommandValidator : AbstractValidator<CreateRetentionPolicyCommand>
{
    public CreateRetentionPolicyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RetentionDays).GreaterThan(0);
        RuleFor(x => x.WarningDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CallerRole).Equal("SystemAdministrator").WithMessage("E-REPORT-FORBIDDEN");
    }
}

public class CreateRetentionPolicyCommandHandler : ICommandHandler<CreateRetentionPolicyCommand, Guid>
{
    private readonly IRetentionPolicyRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateRetentionPolicyCommandHandler(IRetentionPolicyRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result<Guid>> Handle(CreateRetentionPolicyCommand request, CancellationToken ct)
    {
        var scope = RetentionScope.Create(request.ActorRole, new HashSet<ActivityType>());
        var policy = RetentionPolicy.Create(request.Name, scope, request.RetentionDays, request.Action, request.WarningDays);
        if (policy.IsFailure) return Result.Failure<Guid>(policy.Error);
        await _repo.AddAsync(policy.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(policy.Value.Id);
    }
}

public record ArchiveRetentionPolicyCommand(Guid PolicyId) : ICommand;
public class ArchiveRetentionPolicyCommandHandler : ICommandHandler<ArchiveRetentionPolicyCommand>
{
    private readonly IRetentionPolicyRepository _repo; private readonly IUnitOfWork _uow;
    public ArchiveRetentionPolicyCommandHandler(IRetentionPolicyRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(ArchiveRetentionPolicyCommand request, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(request.PolicyId, ct);
        if (p is null) return Result.Failure(new Error("RetentionPolicy.NotFound", "Not found."));
        var r = p.Archive(); if (r.IsFailure) return r;
        _repo.Update(p); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}

public record ApplyRetentionPolicyCommand(Guid PolicyId) : ICommand;
public class ApplyRetentionPolicyCommandHandler : ICommandHandler<ApplyRetentionPolicyCommand>
{
    private readonly IRetentionPolicyRepository _repo;
    private readonly IActivityReadStore _activityStore;
    private readonly IColdStorageArchive _coldStorage;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;
    public ApplyRetentionPolicyCommandHandler(IRetentionPolicyRepository repo, IActivityReadStore activityStore, IColdStorageArchive coldStorage, IClock clock, IUnitOfWork uow)
    { _repo = repo; _activityStore = activityStore; _coldStorage = coldStorage; _clock = clock; _uow = uow; }

    public async Task<Result> Handle(ApplyRetentionPolicyCommand request, CancellationToken ct)
    {
        var policy = await _repo.GetByIdAsync(request.PolicyId, ct);
        if (policy is null) return Result.Failure(new Error("RetentionPolicy.NotFound", "Not found."));

        var now = _clock.UtcNow;
        var cutoff = RetentionCutoffCalculator.ComputeCutoff(policy, now);
        var records = await _activityStore.GetOlderThanAsync(cutoff, policy.Scope, ct);
        var ids = records.Select(r => r.Id).ToList();

        if (policy.Action == RetentionAction.Archive)
            await _coldStorage.ArchiveActivityRecordsAsync(ids, ct);

        await _activityStore.DeleteAsync(ids, ct);

        policy.RecordRun(policy.CurrentVersionNumber, cutoff, ids.Count, policy.Action, now);
        _repo.Update(policy);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
