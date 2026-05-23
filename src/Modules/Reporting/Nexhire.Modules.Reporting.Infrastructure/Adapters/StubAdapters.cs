using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Infrastructure.Adapters;

public class StubReportRenderer : IReportRenderer
{
    public Task<Result<RenderedArtifact>> RenderAsync(ReportRenderRequest request, ExportFormat format, CancellationToken ct = default)
    {
        var content = format switch
        {
            ExportFormat.Csv => System.Text.Encoding.UTF8.GetBytes("col1,col2\nval1,val2"),
            ExportFormat.Xlsx => System.Text.Encoding.UTF8.GetBytes("XLSX-PLACEHOLDER"),
            ExportFormat.Pdf => System.Text.Encoding.UTF8.GetBytes("PDF-PLACEHOLDER"),
            _ => System.Text.Encoding.UTF8.GetBytes("UNKNOWN")
        };
        var mimeType = format switch { ExportFormat.Pdf => "application/pdf", ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _ => "text/csv" };
        return Task.FromResult(Result.Success(new RenderedArtifact(content, mimeType, null)));
    }
}

public class InMemoryObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, byte[]> _store = new();

    public Task<Result<StoredFileReference>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken ct = default)
    {
        var key = $"reports/{Guid.NewGuid()}/{fileName}";
        _store[key] = content;
        return Task.FromResult(Result.Success(new StoredFileReference(key, fileName, mimeType, content.LongLength)));
    }

    public Task<Result<byte[]>> RetrieveAsync(string storageKey, CancellationToken ct = default)
        => _store.TryGetValue(storageKey, out var bytes)
            ? Task.FromResult(Result.Success(bytes))
            : Task.FromResult(Result.Failure<byte[]>(new Shared.Core.Results.Error("ObjectStorage.NotFound", "File not found.")));

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        _store.Remove(storageKey);
        return Task.CompletedTask;
    }
}

public class StubColdStorageArchive : IColdStorageArchive
{
    public Task<Result> ArchiveActivityRecordsAsync(List<Guid> activityRecordIds, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
