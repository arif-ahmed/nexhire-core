using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;

public class EmployerProfilesDbContext : DbContext, IOutboxInboxDbContext
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("employer_profile");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployerProfilesDbContext).Assembly);
        ConfigureOutboxInbox(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    private static void ConfigureOutboxInbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Content).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.OccurredOnUtc).IsRequired();
            builder.Property(x => x.ProcessedOnUtc);
            builder.Property(x => x.Error).HasMaxLength(4000);
            builder.HasIndex(x => x.ProcessedOnUtc);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
            builder.Property(x => x.ReceivedOnUtc).IsRequired();
            builder.Property(x => x.ProcessedOnUtc);
        });
    }
}
