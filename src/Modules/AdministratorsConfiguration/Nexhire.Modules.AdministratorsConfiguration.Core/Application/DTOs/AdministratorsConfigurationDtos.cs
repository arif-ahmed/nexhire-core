namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;

public sealed record ImportRowResultDto(int RowNumber, bool Succeeded, string? ErrorCode, string? Message);

public sealed record ImportResultDto(IReadOnlyList<ImportRowResultDto> Rows, int SucceededCount, int FailedCount);

public sealed record TaxonomyTermNodeDto(
    string Code,
    string Label,
    string? Category,
    string Status,
    int UsageCount,
    IReadOnlyCollection<TaxonomyTermNodeDto> Children);

public sealed record TaxonomyDto(
    string Kind,
    string Name,
    int Version,
    IReadOnlyCollection<TaxonomyTermNodeDto> Terms);

public sealed record TaxonomyTermDetailDto(
    string Code,
    string Kind,
    string Label,
    string? Category,
    string? ParentCode,
    string Status,
    string? ReplacedByCode,
    int UsageCount,
    DateTime CreatedOnUtc,
    DateTime? DeprecatedOnUtc,
    IReadOnlyCollection<string> ChildrenCodes);

public sealed record TermUsageDto(string Code, string Label, int UsageCount);
