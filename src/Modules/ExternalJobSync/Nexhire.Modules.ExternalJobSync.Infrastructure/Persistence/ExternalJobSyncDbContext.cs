using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Persistence;

public sealed class ExternalJobSyncDbContext : DbContext, IOutboxInboxDbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public ExternalJobSyncDbContext(
        DbContextOptions<ExternalJobSyncDbContext> options, 
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<ExternalConnector> Connectors => Set<ExternalConnector>();
    public DbSet<MappingProfile> MappingProfiles => Set<MappingProfile>();
    public DbSet<SyncRecord> SyncRecords => Set<SyncRecord>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<GovernmentAuditEntry> GovernmentAuditEntries => Set<GovernmentAuditEntry>();
    public DbSet<ApiVersionRegistry> ApiVersionRegistries => Set<ApiVersionRegistry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("external_job_sync");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExternalJobSyncDbContext).Assembly);
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
