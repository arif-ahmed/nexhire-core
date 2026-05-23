using Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Queries;

public sealed class GetTaxonomyQueryHandler : IQueryHandler<GetTaxonomyQuery, TaxonomyDto>
{
    private readonly ITaxonomyRepository _repository;

    public GetTaxonomyQueryHandler(ITaxonomyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaxonomyDto>> Handle(GetTaxonomyQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure<TaxonomyDto>(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null)
        {
            return Result.Failure<TaxonomyDto>(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));
        }

        // Map hierarchical terms list
        var termNodes = BuildHierarchicalTree(taxonomy.Terms);

        var dto = new TaxonomyDto(
            taxonomy.Kind.ToString(),
            taxonomy.Name,
            taxonomy.Version,
            termNodes);

        return Result.Success(dto);
    }

    private static IReadOnlyList<TaxonomyTermNodeDto> BuildHierarchicalTree(IEnumerable<TaxonomyTerm> terms)
    {
        var allTerms = terms.ToList();
        var rootTerms = allTerms.Where(t => t.ParentCode == null).ToList();
        var childrenLookup = allTerms.Where(t => t.ParentCode != null)
                                     .GroupBy(t => t.ParentCode!.Value)
                                     .ToDictionary(g => g.Key, g => g.ToList());

        var nodes = new List<TaxonomyTermNodeDto>();
        foreach (var root in rootTerms)
        {
            nodes.Add(BuildNode(root, childrenLookup));
        }

        return nodes;
    }

    private static TaxonomyTermNodeDto BuildNode(TaxonomyTerm term, Dictionary<string, List<TaxonomyTerm>> childrenLookup)
    {
        var childrenNodes = new List<TaxonomyTermNodeDto>();
        if (childrenLookup.TryGetValue(term.Code.Value, out var children))
        {
            foreach (var child in children)
            {
                childrenNodes.Add(BuildNode(child, childrenLookup));
            }
        }

        return new TaxonomyTermNodeDto(
            term.Code.Value,
            term.Label,
            term.Category?.ToString(),
            term.Status.ToString(),
            term.UsageCount,
            childrenNodes);
    }
}

public sealed class GetTaxonomyTermQueryHandler : IQueryHandler<GetTaxonomyTermQuery, TaxonomyTermDetailDto>
{
    private readonly ITaxonomyRepository _repository;

    public GetTaxonomyTermQueryHandler(ITaxonomyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaxonomyTermDetailDto>> Handle(GetTaxonomyTermQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure<TaxonomyTermDetailDto>(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var codeResult = TermCode.Create(request.Code);
        if (codeResult.IsFailure) return Result.Failure<TaxonomyTermDetailDto>(codeResult.Error);

        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Result.Failure<TaxonomyTermDetailDto>(new Error("E-TAXO-NOT-FOUND", "Taxonomy not found."));

        var term = taxonomy.Terms.FirstOrDefault(t => t.Code == codeResult.Value);
        if (term == null)
        {
            return Result.Failure<TaxonomyTermDetailDto>(new Error("E-TAXO-TERM-NOT-FOUND", "Taxonomy term not found."));
        }

        var childrenCodes = taxonomy.Terms.Where(t => t.ParentCode == term.Code)
                                          .Select(t => t.Code.Value)
                                          .ToList();

        var dto = new TaxonomyTermDetailDto(
            term.Code.Value,
            taxonomy.Kind.ToString(),
            term.Label,
            term.Category?.ToString(),
            term.ParentCode?.Value,
            term.Status.ToString(),
            term.ReplacedByCode?.Value,
            term.UsageCount,
            term.CreatedOnUtc,
            term.DeprecatedOnUtc,
            childrenCodes);

        return Result.Success(dto);
    }
}

public sealed class SearchTaxonomyTermsQueryHandler : IQueryHandler<SearchTaxonomyTermsQuery, IReadOnlyCollection<TaxonomyTermDetailDto>>
{
    private readonly ITaxonomyRepository _repository;

    public SearchTaxonomyTermsQueryHandler(ITaxonomyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<TaxonomyTermDetailDto>>> Handle(SearchTaxonomyTermsQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure<IReadOnlyCollection<TaxonomyTermDetailDto>>(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        SkillCategory? category = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            if (Enum.TryParse<SkillCategory>(request.Category, true, out var parsedCategory))
            {
                category = parsedCategory;
            }
        }

        TermStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<TermStatus>(request.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }
        }

        var matchedTerms = await _repository.SearchTermsAsync(kind, request.SearchTerm, category, status, cancellationToken);
        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);

        var dtos = matchedTerms.Select(term =>
        {
            var childrenCodes = taxonomy?.Terms.Where(t => t.ParentCode == term.Code)
                                               .Select(t => t.Code.Value)
                                               .ToList() ?? new List<string>();

            return new TaxonomyTermDetailDto(
                term.Code.Value,
                kind.ToString(),
                term.Label,
                term.Category?.ToString(),
                term.ParentCode?.Value,
                term.Status.ToString(),
                term.ReplacedByCode?.Value,
                term.UsageCount,
                term.CreatedOnUtc,
                term.DeprecatedOnUtc,
                childrenCodes);
        }).ToList();

        return Result.Success<IReadOnlyCollection<TaxonomyTermDetailDto>>(dtos);
    }
}

public sealed class GetTaxonomyUsageStatsQueryHandler : IQueryHandler<GetTaxonomyUsageStatsQuery, IReadOnlyCollection<TermUsageDto>>
{
    private readonly ITaxonomyRepository _repository;

    public GetTaxonomyUsageStatsQueryHandler(ITaxonomyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<TermUsageDto>>> Handle(GetTaxonomyUsageStatsQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaxonomyKind>(request.Kind, true, out var kind))
        {
            return Result.Failure<IReadOnlyCollection<TermUsageDto>>(new Error("E-TAXO-INVALID-KIND", "Invalid taxonomy kind."));
        }

        var terms = await _repository.GetTermsByUsageDescAsync(kind, cancellationToken);

        var dtos = terms.Select(t => new TermUsageDto(t.Code.Value, t.Label, t.UsageCount)).ToList();
        return Result.Success<IReadOnlyCollection<TermUsageDto>>(dtos);
    }
}
