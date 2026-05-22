using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.JobPostings.Infrastructure.Persistence;

public sealed class JobPostingsDbContext : DbContext, IOutboxInboxDbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public JobPostingsDbContext(DbContextOptions<JobPostingsDbContext> options, PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<PostingAuditTrail> PostingAuditTrails => Set<PostingAuditTrail>();
    public DbSet<EmployerStanding> EmployerStandings => Set<EmployerStanding>();
    public DbSet<PostingMetrics> PostingMetrics => Set<PostingMetrics>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job_postings");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobPostingsDbContext).Assembly);
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
