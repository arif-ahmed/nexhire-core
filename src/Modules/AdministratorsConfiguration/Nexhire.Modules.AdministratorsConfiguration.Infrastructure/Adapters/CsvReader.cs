using System.Text;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Ports;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Adapters;

public sealed class CsvReader : ICsvReader
{
    public Result<IReadOnlyList<RawImportRow>> Read(Stream csvContent)
    {
        try
        {
            var rows = new List<RawImportRow>();
            using var reader = new StreamReader(csvContent, Encoding.UTF8);
            
            var lineCount = 0;
            var isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineCount++;

                if (string.IsNullOrWhiteSpace(line)) continue;

                // Skip header line if present
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (line.Contains("Code", StringComparison.OrdinalIgnoreCase) || 
                        line.Contains("Label", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                var fields = ParseCsvLine(line);
                if (fields.Count < 2)
                {
                    return Result.Failure<IReadOnlyList<RawImportRow>>(new Error("CsvReader.InvalidRow", $"Row {lineCount} is invalid. A CSV row must contain at least a Code and a Label."));
                }

                var code = fields[0];
                var label = fields[1];
                
                string? category = fields.Count > 2 && !string.IsNullOrWhiteSpace(fields[2]) ? fields[2] : null;
                string? parentCode = fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3]) ? fields[3] : null;

                rows.Add(new RawImportRow(lineCount, code, label, category, parentCode));
            }

            return Result.Success<IReadOnlyList<RawImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<RawImportRow>>(new Error("CsvReader.ReadError", $"Failed to parse CSV file: {ex.Message}"));
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Handle escaped quotes
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString().Trim());
        return result;
    }
}
