using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence;

public class SearchDiscoveryDbContext : DbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public SearchDiscoveryDbContext(
        DbContextOptions<SearchDiscoveryDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<JobIndexEntry> JobIndexEntries => Set<JobIndexEntry>();
    public DbSet<FavoriteJob> FavoriteJobs => Set<FavoriteJob>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<SearchSession> SearchSessions => Set<SearchSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("search_discovery");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDiscoveryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
