using System.Text;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Infrastructure.Adapters;

public sealed class CsvAuditTrailExporter : IAuditTrailExporter
{
    private readonly IPostingAuditTrailRepository _auditTrails;

    public CsvAuditTrailExporter(IPostingAuditTrailRepository auditTrails)
    {
        _auditTrails = auditTrails;
    }

    public async Task<Result<ExportedAuditTrail>> ExportAsync(Guid jobPostingId, string format, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ExportedAuditTrail>(new Error("E-POST-EXPORT-FORMAT-UNSUPPORTED", "Only CSV and PDF audit exports are supported."));
        }

        var trail = await _auditTrails.GetByPostingIdAsync(jobPostingId, cancellationToken);
        if (trail is null)
        {
            return Result.Failure<ExportedAuditTrail>(new Error("E-POST-NOT-FOUND", "Audit trail was not found."));
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success(new ExportedAuditTrail(
                $"job-posting-{jobPostingId}-audit.pdf",
                "application/pdf",
                BuildSimplePdf(trail.Entries.OrderBy(x => x.OccurredOnUtc).Select(x => $"{x.OccurredOnUtc:O} {x.Kind} {x.Actor.Kind} {x.StatusTransition?.From}->{x.StatusTransition?.To} {x.Reason}").ToArray())));
        }

        var sb = new StringBuilder();
        sb.AppendLine("occurredOnUtc,kind,actorKind,fromStatus,toStatus,changedFields,reason");
        foreach (var entry in trail.Entries.OrderBy(x => x.OccurredOnUtc))
        {
            sb.AppendLine(string.Join(',',
                entry.OccurredOnUtc.ToString("O"),
                entry.Kind,
                entry.Actor.Kind,
                entry.StatusTransition?.From.ToString() ?? string.Empty,
                entry.StatusTransition?.To.ToString() ?? string.Empty,
                Quote(string.Join('|', entry.ChangedFields)),
                Quote(entry.Reason ?? string.Empty)));
        }

        return Result.Success(new ExportedAuditTrail($"job-posting-{jobPostingId}-audit.csv", "text/csv", Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static byte[] BuildSimplePdf(IReadOnlyCollection<string> lines)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT /F1 10 Tf 40 760 Td");
        foreach (var line in lines.DefaultIfEmpty("No audit entries.").Take(45))
        {
            contentBuilder.AppendLine($"({EscapePdfText(line)}) Tj");
            contentBuilder.AppendLine("0 -14 Td");
        }
        contentBuilder.AppendLine("ET");

        var content = contentBuilder.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };

        var builder = new StringBuilder();
        builder.AppendLine("%PDF-1.4");
        var offsets = new List<int> { 0 };
        foreach (var (body, index) in objects.Select((body, index) => (body, index)))
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.AppendLine($"{index + 1} 0 obj");
            builder.AppendLine(body);
            builder.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.AppendLine("xref");
        builder.AppendLine($"0 {objects.Length + 1}");
        builder.AppendLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            builder.AppendLine($"{offset:0000000000} 00000 n ");
        }
        builder.AppendLine("trailer");
        builder.AppendLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        builder.AppendLine("startxref");
        builder.AppendLine(xrefOffset.ToString());
        builder.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
