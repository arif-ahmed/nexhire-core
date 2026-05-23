using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;

public sealed record RawImportRow(int RowNumber, string Code, string Label, string? Category, string? ParentCode);

public sealed record ImportRowResult(int RowNumber, bool Succeeded, string? ErrorCode, string? Message);

public sealed record ImportResult(IReadOnlyList<ImportRowResult> Rows, int SucceededCount, int FailedCount);

public sealed class TaxonomyImportService
{
    public ImportResult ValidateAndStage(Taxonomy taxonomy, IReadOnlyList<RawImportRow> rows)
    {
        var rowResults = new List<ImportRowResult>();
        var stagedCodesInBatch = new HashSet<string>();

        var uniqueRows = new List<RawImportRow>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code))
            {
                uniqueRows.Add(row);
                continue;
            }

            var normalizedCode = row.Code.Trim().ToUpperInvariant();
            if (seenCodes.Contains(normalizedCode))
            {
                rowResults.Add(new ImportRowResult(
                    row.RowNumber,
                    false,
                    "E-TAXO-DUPLICATE-CODE",
                    "A term with this code is duplicated within the import batch."));
            }
            else
            {
                seenCodes.Add(normalizedCode);
                uniqueRows.Add(row);
            }
        }

        // 1. Topological Sorting / Dependency Ordering of the batch
        var sortedRows = TryTopologicalSort(uniqueRows, out var sortErrorRows);
        foreach (var errorRow in sortErrorRows)
        {
            rowResults.Add(new ImportRowResult(
                errorRow.RowNumber,
                false,
                "E-TAXO-CYCLE-IN-BATCH",
                "A circular dependency was detected within the import batch rows."));
        }

        // 2. Process rows in topological order
        foreach (var row in sortedRows)
        {
            // Parse Code
            var termCodeResult = TermCode.Create(row.Code);
            if (termCodeResult.IsFailure)
            {
                rowResults.Add(new ImportRowResult(
                    row.RowNumber,
                    false,
                    termCodeResult.Error.Code,
                    termCodeResult.Error.Message));
                continue;
            }

            var termCode = termCodeResult.Value;

            // Parse Category (if provided)
            SkillCategory? category = null;
            if (!string.IsNullOrWhiteSpace(row.Category))
            {
                if (Enum.TryParse<SkillCategory>(row.Category, true, out var parsedCategory))
                {
                    category = parsedCategory;
                }
                else
                {
                    rowResults.Add(new ImportRowResult(
                        row.RowNumber,
                        false,
                        "E-TAXO-INVALID-CATEGORY",
                        $"Category '{row.Category}' is invalid. Allowed values are Hard or Soft."));
                    continue;
                }
            }

            // Parse ParentCode (if provided)
            TermCode? parentCode = null;
            if (!string.IsNullOrWhiteSpace(row.ParentCode))
            {
                var parentCodeResult = TermCode.Create(row.ParentCode);
                if (parentCodeResult.IsFailure)
                {
                    rowResults.Add(new ImportRowResult(
                        row.RowNumber,
                        false,
                        parentCodeResult.Error.Code,
                        $"Parent code invalid: {parentCodeResult.Error.Message}"));
                    continue;
                }
                parentCode = parentCodeResult.Value;
            }

            // Stage onto the aggregate (no version bump per row)
            var stageResult = taxonomy.TryAddTermForImport(termCode, row.Label, category, parentCode);
            if (stageResult.IsFailure)
            {
                rowResults.Add(new ImportRowResult(
                    row.RowNumber,
                    false,
                    stageResult.Error.Code,
                    stageResult.Error.Message));
            }
            else
            {
                stagedCodesInBatch.Add(termCode.Value);
                rowResults.Add(new ImportRowResult(
                    row.RowNumber,
                    true,
                    null,
                    "Staged successfully."));
            }
        }

        // Maintain original row order in output results for caller mapping
        var orderedResults = rowResults.OrderBy(r => r.RowNumber).ToList();
        var succeededCount = orderedResults.Count(r => r.Succeeded);
        var failedCount = orderedResults.Count - succeededCount;

        return new ImportResult(orderedResults, succeededCount, failedCount);
    }

    private static IReadOnlyList<RawImportRow> TryTopologicalSort(IReadOnlyList<RawImportRow> rows, out List<RawImportRow> cycleRows)
    {
        var rowLookup = new Dictionary<string, RawImportRow>();
        foreach (var r in rows)
        {
            var key = r.Code.Trim().ToUpperInvariant();
            rowLookup.TryAdd(key, r);
        }
        var visited = new Dictionary<string, bool>(); // false = visiting, true = fully processed
        var sortedList = new List<RawImportRow>();
        cycleRows = new List<RawImportRow>();

        foreach (var row in rows)
        {
            var code = row.Code.Trim().ToUpperInvariant();
            if (!visited.ContainsKey(code))
            {
                if (!Visit(row, rowLookup, visited, sortedList, cycleRows))
                {
                    // If a cycle is detected, we'll continue trying to process other nodes,
                    // but the cyclic ones will be added to cycleRows.
                }
            }
        }

        return sortedList;
    }

    private static bool Visit(
        RawImportRow row,
        Dictionary<string, RawImportRow> rowLookup,
        Dictionary<string, bool> visited,
        List<RawImportRow> sortedList,
        List<RawImportRow> cycleRows)
    {
        var code = row.Code.Trim().ToUpperInvariant();
        visited[code] = false; // Visiting

        if (!string.IsNullOrWhiteSpace(row.ParentCode))
        {
            var parentKey = row.ParentCode.Trim().ToUpperInvariant();
            if (rowLookup.TryGetValue(parentKey, out var parentRow))
            {
                if (visited.TryGetValue(parentKey, out var isVisited))
                {
                    if (!isVisited)
                    {
                        // Cycle detected!
                        cycleRows.Add(row);
                        visited[code] = true; // Mark as processed to avoid infinite loop
                        return false;
                    }
                }
                else
                {
                    if (!Visit(parentRow, rowLookup, visited, sortedList, cycleRows))
                    {
                        cycleRows.Add(row);
                        visited[code] = true;
                        return false;
                    }
                }
            }
        }

        visited[code] = true; // Fully processed
        sortedList.Add(row);
        return true;
    }
}
