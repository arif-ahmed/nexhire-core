using Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Commands;

public sealed record SeedTaxonomiesCommand() : ICommand;

public sealed record AddTaxonomyTermCommand(
    string Kind,
    string Code,
    string Label,
    string? Category,
    string? ParentCode) : ICommand;

public sealed record RenameTaxonomyTermCommand(
    string Kind,
    string Code,
    string NewLabel) : ICommand;

public sealed record RecategorizeSkillCommand(
    string Code,
    string NewCategory) : ICommand;

public sealed record ReparentTaxonomyTermCommand(
    string Kind,
    string Code,
    string? NewParentCode) : ICommand;

public sealed record DeprecateTaxonomyTermCommand(
    string Kind,
    string Code,
    string? ReplacedByCode) : ICommand;

public sealed record ReactivateTaxonomyTermCommand(
    string Kind,
    string Code) : ICommand;

public sealed record BulkImportTaxonomyCommand(
    string Kind,
    Stream CsvStream) : ICommand<ImportResultDto>;
