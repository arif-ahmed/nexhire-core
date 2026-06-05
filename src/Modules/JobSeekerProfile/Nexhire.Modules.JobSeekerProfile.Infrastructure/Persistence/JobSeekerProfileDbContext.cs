using Microsoft.EntityFrameworkCore;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence;

public class JobSeekerProfileDbContext : DbContext, IOutboxInboxDbContext
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

    // IOutboxInboxDbContext
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job_seeker_profile");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobSeekerProfileDbContext).Assembly);
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
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
            b.Property(x => x.Content).HasColumnName("content").IsRequired();
            b.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
            b.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
            b.Property(x => x.Error).HasColumnName("error");
            b.HasIndex(x => x.ProcessedOnUtc);
        });

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
            b.Property(x => x.ReceivedOnUtc).HasColumnName("received_on_utc").IsRequired();
            b.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
        });
    }
}
