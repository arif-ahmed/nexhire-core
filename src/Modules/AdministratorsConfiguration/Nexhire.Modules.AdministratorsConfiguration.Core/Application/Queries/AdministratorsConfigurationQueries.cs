using Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Queries;

public sealed record GetTaxonomyQuery(string Kind) : IQuery<TaxonomyDto>;

public sealed record GetTaxonomyTermQuery(string Kind, string Code) : IQuery<TaxonomyTermDetailDto>;

public sealed record SearchTaxonomyTermsQuery(
    string Kind,
    string? SearchTerm,
    string? Category,
    string? Status) : IQuery<IReadOnlyCollection<TaxonomyTermDetailDto>>;

public sealed record GetTaxonomyUsageStatsQuery(string Kind) : IQuery<IReadOnlyCollection<TermUsageDto>>;
