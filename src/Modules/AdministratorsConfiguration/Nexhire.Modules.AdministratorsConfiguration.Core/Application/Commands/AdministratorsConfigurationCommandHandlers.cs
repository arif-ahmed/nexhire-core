using Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Ports;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Commands;

public sealed class SeedTaxonomiesCommandHandler : ICommandHandler<SeedTaxonomiesCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public SeedTaxonomiesCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SeedTaxonomiesCommand request, CancellationToken cancellationToken)
    {
        foreach (TaxonomyKind kind in Enum.GetValues<TaxonomyKind>())
        {
            var existing = await _repository.GetByKindAsync(kind, cancellationToken);
            if (existing == null)
            {
                var name = kind switch
                {
                    TaxonomyKind.Skills => "Skills Taxonomy",
                    TaxonomyKind.Occupations => "Occupations Taxonomy",
                    TaxonomyKind.TrainingPrograms => "Training Programs Taxonomy",
                    _ => $"{kind} Taxonomy"
                };

                var taxonomy = Taxonomy.Create(kind, name);
                await _repository.AddAsync(taxonomy, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AddTaxonomyTermCommandHandler : ICommandHandler<AddTaxonomyTermCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public AddTaxonomyTermCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddTaxonomyTermCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-KIND", $"Taxonomy kind '{request.Kind}' is invalid."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        SkillCategory? category = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            if (!Enum.TryParse<SkillCategory>(request.Category, true, out var parsedCategory))
            {
                return Result.Failure(new Error("E-TAXO-INVALID-CATEGORY", "Skill category is invalid."));
            }
            category = parsedCategory;
        }

        TermCode? parentCode = null;
        if (!string.IsNullOrWhiteSpace(request.ParentCode))
        {
            var parentResult = TermCode.Create(request.ParentCode);
            if (parentResult.IsFailure) return Result.Failure(parentResult.Error);
            parentCode = parentResult.Value;
        }

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null)
        {
            return Result.Failure(new Error("E-TAXO-NOT-FOUND", $"Taxonomy of kind '{kind}' was not found. Please run seed first."));
        }

        var result = taxonomy.AddTerm(codeResult.Value, request.Label, category, parentCode);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RenameTaxonomyTermCommandHandler : ICommandHandler<RenameTaxonomyTermCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public RenameTaxonomyTermCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RenameTaxonomyTermCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Result.Failure(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));

        var result = taxonomy.RenameTerm(codeResult.Value, request.NewLabel);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RecategorizeSkillCommandHandler : ICommandHandler<RecategorizeSkillCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public RecategorizeSkillCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecategorizeSkillCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        if (!Enum.TryParse<SkillCategory>(request.NewCategory, true, out var category))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-CATEGORY", "New skill category is invalid."));
        }

        var taxonomy = await _repository.GetByKindAsync(TaxonomyKind.Skills, cancellationToken);
        if (taxonomy == null) return Result.Failure(new Error("E-TAXO-NOT-FOUND", "Skills taxonomy not found."));

        var result = taxonomy.RecategorizeTerm(codeResult.Value, category);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class ReparentTaxonomyTermCommandHandler : ICommandHandler<ReparentTaxonomyTermCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public ReparentTaxonomyTermCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReparentTaxonomyTermCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        TermCode? parentCode = null;
        if (!string.IsNullOrWhiteSpace(request.NewParentCode))
        {
            var parentResult = TermCode.Create(request.NewParentCode);
            if (parentResult.IsFailure) return Result.Failure(parentResult.Error);
            parentCode = parentResult.Value;
        }

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Result.Failure(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));

        var result = taxonomy.ReparentTerm(codeResult.Value, parentCode);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeprecateTaxonomyTermCommandHandler : ICommandHandler<DeprecateTaxonomyTermCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public DeprecateTaxonomyTermCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeprecateTaxonomyTermCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        TermCode? replacedByCode = null;
        if (!string.IsNullOrWhiteSpace(request.ReplacedByCode))
        {
            var replacementResult = TermCode.Create(request.ReplacedByCode);
            if (replacementResult.IsFailure) return Result.Failure(replacementResult.Error);
            replacedByCode = replacementResult.Value;
        }

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Result.Failure(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));

        var result = taxonomy.DeprecateTerm(codeResult.Value, replacedByCode);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class ReactivateTaxonomyTermCommandHandler : ICommandHandler<ReactivateTaxonomyTermCommand>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public ReactivateTaxonomyTermCommandHandler(ITaxonomyRepository repository, IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReactivateTaxonomyTermCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Result.Failure(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));

        var result = taxonomy.ReactivateTerm(codeResult.Value);
        if (result.IsFailure) return result;

        _repository.Update(taxonomy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class BulkImportTaxonomyCommandHandler : ICommandHandler<BulkImportTaxonomyCommand, ImportResultDto>
{
    private readonly ITaxonomyRepository _repository;
    private readonly ICsvReader _csvReader;
    private readonly TaxonomyImportService _importService;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWork;

    public BulkImportTaxonomyCommandHandler(
        ITaxonomyRepository repository,
        ICsvReader csvReader,
        TaxonomyImportService importService,
        IAdministratorsConfigurationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _csvReader = csvReader;
        _importService = importService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ImportResultDto>> Handle(BulkImportTaxonomyCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure<ImportResultDto>(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var parseResult = _csvReader.Read(request.CsvStream);
        if (parseResult.IsFailure) return Result.Failure<ImportResultDto>(parseResult.Error);

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null)
        {
            return Result.Failure<ImportResultDto>(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found. Please seed first."));
        }

        var importResult = _importService.ValidateAndStage(taxonomy, parseResult.Value);

        if (importResult.SucceededCount > 0)
        {
            taxonomy.FinalizeImport();
            _repository.Update(taxonomy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dtoRows = importResult.Rows.Select(r => new ImportRowResultDto(
            r.RowNumber,
            r.Succeeded,
            r.ErrorCode,
            r.Message)).ToList();

        var dtoResult = new ImportResultDto(dtoRows, importResult.SucceededCount, importResult.FailedCount);
        return Result.Success(dtoResult);
    }
}
