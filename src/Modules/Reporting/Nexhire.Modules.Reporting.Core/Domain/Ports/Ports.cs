using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Ports;

public record ReportRenderRequest(string ReportName, VisualizationType Visualization,
    List<string> ColumnHeaders, List<List<string>> Rows);

public record RenderedArtifact(byte[] Content, string MimeType, int? PageCount);

public interface IReportRenderer
{
    Task<Result<RenderedArtifact>> RenderAsync(ReportRenderRequest request, ExportFormat format, CancellationToken ct = default);
}

public record StoredFileReference(string StorageKey, string OriginalFileName, string MimeType, long SizeBytes);

public interface IObjectStorage
{
    Task<Result<StoredFileReference>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken ct = default);
    Task<Result<byte[]>> RetrieveAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public interface IColdStorageArchive
{
    Task<Result> ArchiveActivityRecordsAsync(List<Guid> activityRecordIds, CancellationToken ct = default);
}

public interface IClock
{
    DateTime UtcNow { get; }
}
