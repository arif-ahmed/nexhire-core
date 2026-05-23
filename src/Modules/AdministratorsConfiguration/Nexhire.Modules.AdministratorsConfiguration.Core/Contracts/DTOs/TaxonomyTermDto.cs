namespace Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.DTOs;

public sealed record TaxonomyTermDto(
    string TaxonomyCode,
    string Kind,
    string Label,
    string? Category,
    string? ParentCode,
    string Status,
    string? ReplacedByCode,
    int UsageCount);
