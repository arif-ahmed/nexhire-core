using Microsoft.EntityFrameworkCore;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence;

public class JobSeekerProfileDbContext : DbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public JobSeekerProfileDbContext(
        DbContextOptions<JobSeekerProfileDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<Aggregates.JobSeekerProfile> JobSeekerProfiles => Set<Aggregates.JobSeekerProfile>();
    public DbSet<Aggregates.Resume> Resumes => Set<Aggregates.Resume>();
    public DbSet<Aggregates.ProfileHistory> ProfileHistories => Set<Aggregates.ProfileHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job_seeker_profile");
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobSeekerProfileDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
