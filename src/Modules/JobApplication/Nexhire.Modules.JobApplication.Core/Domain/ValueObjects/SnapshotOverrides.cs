namespace Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;

public record SnapshotOverrides(
    string? FullName = null,
    string? Email = null,
    string? Mobile = null,
    string? CurrentLocation = null);
