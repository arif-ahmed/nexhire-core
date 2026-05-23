using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.ReadModels;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.Reporting.Infrastructure.Persistence;

public class ReportingDbContext : DbContext, IOutboxInboxDbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options, PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    // Aggregates
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportDefinitionVersion> ReportDefinitionVersions => Set<ReportDefinitionVersion>();
    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();
    public DbSet<ReportArtifact> ReportArtifacts => Set<ReportArtifact>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<RetentionPolicyVersion> RetentionPolicyVersions => Set<RetentionPolicyVersion>();
    public DbSet<RetentionRun> RetentionRuns => Set<RetentionRun>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertIncident> AlertIncidents => Set<AlertIncident>();

    // Read models
    public DbSet<ActivityRecord> ActivityRecords => Set<ActivityRecord>();
    public DbSet<SessionSnapshot> SessionSnapshots => Set<SessionSnapshot>();
    public DbSet<AnalyticsRollup> AnalyticsRollups => Set<AnalyticsRollup>();
    public DbSet<SalaryStatRollup> SalaryStatRollups => Set<SalaryStatRollup>();
    public DbSet<SkillDemandRollup> SkillDemandRollups => Set<SkillDemandRollup>();
    public DbSet<SystemMetricBucket> SystemMetricBuckets => Set<SystemMetricBucket>();
    public DbSet<MatchingMetricRollup> MatchingMetricRollups => Set<MatchingMetricRollup>();
    public DbSet<OutcomeCohortRollup> OutcomeCohortRollups => Set<OutcomeCohortRollup>();
    public DbSet<ReportAccessLog> ReportAccessLogs => Set<ReportAccessLog>();

    // Outbox/inbox
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
