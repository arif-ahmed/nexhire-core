using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence;

public sealed class JobApplicationDbContext : DbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public JobApplicationDbContext(
        DbContextOptions<JobApplicationDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<IdempotencyKeyEntry> IdempotencyKeys => Set<IdempotencyKeyEntry>();
    public DbSet<WithdrawalReasonLookup> WithdrawalReasons => Set<WithdrawalReasonLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job_application");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
