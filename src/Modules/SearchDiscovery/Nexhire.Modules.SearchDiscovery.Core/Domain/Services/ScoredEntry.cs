namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Services;

public record ScoredEntry(Guid EntryId, double Score, DateTime? PostedOnUtc = null);
