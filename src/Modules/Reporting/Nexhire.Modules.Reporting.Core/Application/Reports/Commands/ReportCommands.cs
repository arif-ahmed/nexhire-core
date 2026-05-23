using FluentValidation;
using MediatR;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Ports;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Core.Domain.Services;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Application.Reports.Commands;

// --- CreateReportTemplate ---
public record CreateReportTemplateCommand(
    string Name, ReportCategory Category, Guid OwnerUserId,
    List<string> Metrics, List<string> Dimensions,
    VisualizationType Visualization, string CallerRole) : ICommand<Guid>;

public class CreateReportTemplateCommandValidator : AbstractValidator<CreateReportTemplateCommand>
{
    public CreateReportTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Metrics).NotEmpty();
        RuleFor(x => x.CallerRole).Must(r => new[] { "SystemAdministrator", "MoLAdministrator", "DataAnalyst" }.Contains(r))
            .WithMessage("E-REPORT-FORBIDDEN");
    }
}

public class CreateReportTemplateCommandHandler : ICommandHandler<CreateReportTemplateCommand, Guid>
{
    private readonly IReportDefinitionRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateReportTemplateCommandHandler(IReportDefinitionRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result<Guid>> Handle(CreateReportTemplateCommand request, CancellationToken ct)
    {
        foreach (var m in request.Metrics)
            if (!MetricCatalog.IsKnownMetric(m))
                return Result.Failure<Guid>(new Error("E-REPORT-FORBIDDEN", $"Unknown metric: {m}"));

        var spec = ReportSpec.CreateUnsafe(request.Metrics, request.Dimensions, new List<ReportFilter>(), request.Visualization);
        var visibility = ReportVisibility.Create(new HashSet<string> { request.CallerRole }).Value;

        var result = ReportDefinition.CreateTemplate(request.Name, request.Category, request.OwnerUserId, spec, new List<ConfigurableParameter>(), visibility);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await _repo.AddAsync(result.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}

// --- CreateCustomReport ---
public record CreateCustomReportCommand(string Name, Guid OwnerUserId, List<string> Metrics, List<string> Dimensions, VisualizationType Visualization) : ICommand<Guid>;

public class CreateCustomReportCommandValidator : AbstractValidator<CreateCustomReportCommand>
{
    public CreateCustomReportCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Metrics).NotEmpty();
    }
}

public class CreateCustomReportCommandHandler : ICommandHandler<CreateCustomReportCommand, Guid>
{
    private readonly IReportDefinitionRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateCustomReportCommandHandler(IReportDefinitionRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result<Guid>> Handle(CreateCustomReportCommand request, CancellationToken ct)
    {
        var spec = ReportSpec.CreateUnsafe(request.Metrics, request.Dimensions, new List<ReportFilter>(), request.Visualization);
        var result = ReportDefinition.CreateCustom(request.Name, request.OwnerUserId, spec);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);
        await _repo.AddAsync(result.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}

// --- ArchiveReportDefinition ---
public record ArchiveReportDefinitionCommand(Guid DefinitionId) : ICommand;

public class ArchiveReportDefinitionCommandHandler : ICommandHandler<ArchiveReportDefinitionCommand>
{
    private readonly IReportDefinitionRepository _repo;
    private readonly IUnitOfWork _uow;
    public ArchiveReportDefinitionCommandHandler(IReportDefinitionRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(ArchiveReportDefinitionCommand request, CancellationToken ct)
    {
        var def = await _repo.GetByIdAsync(request.DefinitionId, ct);
        if (def is null) return Result.Failure(new Error("ReportDefinition.NotFound", "Not found."));
        var result = def.Archive();
        if (result.IsFailure) return result;
        _repo.Update(def);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- GenerateReport ---
public record GenerateReportCommand(Guid DefinitionId, Guid RequestedByUserId, string CallerRole, Guid? CallerEmployerId, List<ExportFormat> Formats, Dictionary<string, string> Parameters) : ICommand<Guid>;

public class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(x => x.DefinitionId).NotEmpty();
        RuleFor(x => x.Formats).NotEmpty();
    }
}

public class GenerateReportCommandHandler : ICommandHandler<GenerateReportCommand, Guid>
{
    private readonly IReportDefinitionRepository _defRepo;
    private readonly IReportRunRepository _runRepo;
    private readonly IUnitOfWork _uow;
    public GenerateReportCommandHandler(IReportDefinitionRepository defRepo, IReportRunRepository runRepo, IUnitOfWork uow)
    { _defRepo = defRepo; _runRepo = runRepo; _uow = uow; }

    public async Task<Result<Guid>> Handle(GenerateReportCommand request, CancellationToken ct)
    {
        var def = await _defRepo.GetByIdAsync(request.DefinitionId, ct);
        if (def is null) return Result.Failure<Guid>(new Error("ReportDefinition.NotFound", "Definition not found."));

        var roleResult = RoleName.Create(request.CallerRole);
        if (roleResult.IsFailure) return Result.Failure<Guid>(new Error("E-REPORT-FORBIDDEN", "Invalid role."));

        var roleScopeResult = RoleScope.Create(roleResult.Value, request.CallerEmployerId);
        if (roleScopeResult.IsFailure) return Result.Failure<Guid>(roleScopeResult.Error);

        var resolvedParams = ResolvedParameters.Create(request.Parameters, def.ConfigurableParameters);
        if (resolvedParams.IsFailure) return Result.Failure<Guid>(new Error("E-REPORT-MISSING-PARAM", resolvedParams.Error.Message));

        var trigger = RunTrigger.CreateOnDemand(request.RequestedByUserId);

        var run = ReportRun.Queue(def.Id, def.CurrentVersionNumber, trigger.Value, resolvedParams.Value, roleScopeResult.Value, request.Formats);
        if (run.IsFailure) return Result.Failure<Guid>(run.Error);

        await _runRepo.AddAsync(run.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(run.Value.Id);
    }
}

// --- ExecuteReportRun (internal, issued by worker) ---
public record ExecuteReportRunCommand(Guid RunId) : ICommand;

public class ExecuteReportRunCommandHandler : ICommandHandler<ExecuteReportRunCommand>
{
    private readonly IReportRunRepository _runRepo;
    private readonly IReportDefinitionRepository _defRepo;
    private readonly IReportRenderer _renderer;
    private readonly IObjectStorage _storage;
    private readonly IUnitOfWork _uow;

    public ExecuteReportRunCommandHandler(IReportRunRepository runRepo, IReportDefinitionRepository defRepo,
        IReportRenderer renderer, IObjectStorage storage, IUnitOfWork uow)
    { _runRepo = runRepo; _defRepo = defRepo; _renderer = renderer; _storage = storage; _uow = uow; }

    public async Task<Result> Handle(ExecuteReportRunCommand request, CancellationToken ct)
    {
        var run = await _runRepo.GetByIdAsync(request.RunId, ct);
        if (run is null) return Result.Failure(new Error("ReportRun.NotFound", "Run not found."));

        var startResult = run.MarkRunning();
        if (startResult.IsFailure) return startResult;

        try
        {
            var renderRequest = new ReportRenderRequest("Report", VisualizationType.Table, new List<string> { "Column1" }, new List<List<string>>());
            var artifacts = new List<(ExportFormat, FileReference)>();

            foreach (var format in run.RequestedFormats)
            {
                var rendered = await _renderer.RenderAsync(renderRequest, format, ct);
                if (rendered.IsFailure) { run.MarkFailed(rendered.Error.Message); _runRepo.Update(run); await _uow.SaveChangesAsync(ct); return Result.Failure(rendered.Error); }

                var mimeType = format switch { ExportFormat.Pdf => "application/pdf", ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _ => "text/csv" };
                var stored = await _storage.StoreAsync(rendered.Value.Content, $"report.{format.ToString().ToLower()}", mimeType, ct);
                if (stored.IsFailure) { run.MarkFailed(stored.Error.Message); _runRepo.Update(run); await _uow.SaveChangesAsync(ct); return Result.Failure(stored.Error); }

                var fileRef = FileReference.Create(stored.Value.StorageKey, stored.Value.OriginalFileName, stored.Value.MimeType, stored.Value.SizeBytes);
                if (fileRef.IsFailure) { run.MarkFailed(fileRef.Error.Message); _runRepo.Update(run); await _uow.SaveChangesAsync(ct); return Result.Failure(fileRef.Error); }
                artifacts.Add((format, fileRef.Value));
            }

            var completeResult = run.MarkCompleted(artifacts, 0);
            if (completeResult.IsFailure) return completeResult;

            var def = await _defRepo.GetByIdAsync(run.ReportDefinitionId, ct);
            def?.RecordUsage();
            if (def is not null) _defRepo.Update(def);

            _runRepo.Update(run);
            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            run.MarkFailed(ex.Message);
            _runRepo.Update(run);
            await _uow.SaveChangesAsync(ct);
            return Result.Failure(new Error("ReportRun.ExecutionFailed", ex.Message));
        }
    }
}
