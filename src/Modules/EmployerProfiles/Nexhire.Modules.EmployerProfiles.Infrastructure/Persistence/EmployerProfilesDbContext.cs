using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;

public class EmployerProfilesDbContext : DbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public EmployerProfilesDbContext(
        DbContextOptions<EmployerProfilesDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();
    public DbSet<Shortlist> Shortlists => Set<Shortlist>();
    public DbSet<DashboardPosting> DashboardPostings => Set<DashboardPosting>();
    public DbSet<DashboardApplication> DashboardApplications => Set<DashboardApplication>();
    public DbSet<DashboardMatchedCandidate> DashboardMatchedCandidates => Set<DashboardMatchedCandidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Segregate this module's data into its own logical schema
        modelBuilder.HasDefaultSchema("employer_profile");
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployerProfilesDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
