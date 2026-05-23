using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ReadModels;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.Reporting.Infrastructure.Persistence.Configurations;

public class ActivityRecordConfiguration : IEntityTypeConfiguration<ActivityRecord>
{
    public void Configure(EntityTypeBuilder<ActivityRecord> builder)
    {
        builder.ToTable("activity_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ActorRole).HasConversion<string>().IsRequired().HasMaxLength(30);
        builder.Property(x => x.ActivityType).HasConversion<string>().IsRequired().HasMaxLength(60);
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(100);
        builder.Property(x => x.Metadata).HasColumnType("TEXT");
        builder.Property(x => x.SourceEventId).IsRequired();
        builder.Property(x => x.ProjectedOnUtc).IsRequired();
        builder.HasIndex(x => new { x.SourceEventId, x.ActivityType }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.OccurredOnUtc });
        builder.HasIndex(x => x.OccurredOnUtc);
    }
}

public class SessionSnapshotConfiguration : IEntityTypeConfiguration<SessionSnapshot>
{
    public void Configure(EntityTypeBuilder<SessionSnapshot> builder)
    {
        builder.ToTable("session_snapshots");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ActorRole).HasConversion<string>().IsRequired().HasMaxLength(30);
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
    }
}

public class AnalyticsRollupConfiguration : IEntityTypeConfiguration<AnalyticsRollup>
{
    public void Configure(EntityTypeBuilder<AnalyticsRollup> builder)
    {
        builder.ToTable("analytics_rollups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Metric).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Grain).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.OccupationCode).HasMaxLength(50);
        builder.Property(x => x.SkillCode).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
        builder.HasIndex(x => new { x.Metric, x.Grain, x.BucketStartUtc });
    }
}

public class SalaryStatRollupConfiguration : IEntityTypeConfiguration<SalaryStatRollup>
{
    public void Configure(EntityTypeBuilder<SalaryStatRollup> builder)
    {
        builder.ToTable("salary_stat_rollups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OccupationCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.Grain).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
    }
}

public class SkillDemandRollupConfiguration : IEntityTypeConfiguration<SkillDemandRollup>
{
    public void Configure(EntityTypeBuilder<SkillDemandRollup> builder)
    {
        builder.ToTable("skill_demand_rollups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SkillCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.Grain).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.HasIndex(x => new { x.SkillCode, x.Grain, x.BucketStartUtc });
    }
}

public class SystemMetricBucketConfiguration : IEntityTypeConfiguration<SystemMetricBucket>
{
    public void Configure(EntityTypeBuilder<SystemMetricBucket> builder)
    {
        builder.ToTable("system_metric_buckets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MetricKey).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => new { x.MetricKey, x.BucketStartUtc });
    }
}

public class MatchingMetricRollupConfiguration : IEntityTypeConfiguration<MatchingMetricRollup>
{
    public void Configure(EntityTypeBuilder<MatchingMetricRollup> builder)
    {
        builder.ToTable("matching_metric_rollups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Grain).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(x => x.JobCategory).HasMaxLength(100);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.SkillCode).HasMaxLength(100);
        builder.Property(x => x.AbTestVariant).HasMaxLength(100);
    }
}

public class OutcomeCohortRollupConfiguration : IEntityTypeConfiguration<OutcomeCohortRollup>
{
    public void Configure(EntityTypeBuilder<OutcomeCohortRollup> builder)
    {
        builder.ToTable("outcome_cohort_rollups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Grain).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.SkillCode).HasMaxLength(100);
    }
}

public class ReportAccessLogConfiguration : IEntityTypeConfiguration<ReportAccessLog>
{
    public void Configure(EntityTypeBuilder<ReportAccessLog> builder)
    {
        builder.ToTable("report_access_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Role).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(20);
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.OccurredOnUtc });
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
    }
}

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.Id);
    }
}
