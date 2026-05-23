using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.Reporting.Infrastructure.Persistence.Configurations;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.Spec).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ReportSpec>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.ConfigurableParameters).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ConfigurableParameter>>(v, (JsonSerializerOptions?)null) ?? new()).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.Visibility).HasConversion(
            v => JsonSerializer.Serialize(v.AllowedRoles, (JsonSerializerOptions?)null),
            v => ReportVisibility.Create(JsonSerializer.Deserialize<HashSet<string>>(v, (JsonSerializerOptions?)null) ?? new()).Value).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.CurrentVersionNumber).IsRequired();
        builder.Property(x => x.UsageCount).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Versions).WithOne().HasForeignKey(v => v.DefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => new { x.Kind, x.Status });
    }
}

public class ReportDefinitionVersionConfiguration : IEntityTypeConfiguration<ReportDefinitionVersion>
{
    public void Configure(EntityTypeBuilder<ReportDefinitionVersion> builder)
    {
        builder.ToTable("report_definition_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DefinitionId).IsRequired();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.Spec).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ReportSpec>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.ChangedBy).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.HasIndex(x => new { x.DefinitionId, x.VersionNumber }).IsUnique();
    }
}

public class ReportRunConfiguration : IEntityTypeConfiguration<ReportRun>
{
    public void Configure(EntityTypeBuilder<ReportRun> builder)
    {
        builder.ToTable("report_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportDefinitionId).IsRequired();
        builder.Property(x => x.DefinitionVersionNumber).IsRequired();
        builder.Property(x => x.Trigger).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<RunTrigger>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.Parameters).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ResolvedParameters>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.RoleScope).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<RoleScope>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.RequestedFormats).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ExportFormat>>(v, (JsonSerializerOptions?)null) ?? new()).HasColumnType("TEXT");
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Artifacts).WithOne().HasForeignKey(a => a.ReportRunId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Artifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ReportDefinitionId);
    }
}

public class ReportArtifactConfiguration : IEntityTypeConfiguration<ReportArtifact>
{
    public void Configure(EntityTypeBuilder<ReportArtifact> builder)
    {
        builder.ToTable("report_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportRunId).IsRequired();
        builder.Property(x => x.Format).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(x => x.File).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<FileReference>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
    }
}

public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("report_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportDefinitionId).IsRequired();
        builder.Property(x => x.Cadence).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ScheduleCadence>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.Parameters).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ResolvedParameters>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.DistributionList).HasConversion(
            v => JsonSerializer.Serialize(v.Select(e => e.Value).ToList(), (JsonSerializerOptions?)null),
            v => (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()).Select(e => EmailAddress.Create(e).Value).ToList()).HasColumnType("TEXT");
        builder.Property(x => x.ExportFormats).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ExportFormat>>(v, (JsonSerializerOptions?)null) ?? new()).HasColumnType("TEXT");
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Navigation(x => x.DistributionList).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ExportFormats).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.NextRunOnUtc);
    }
}

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Scope).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<RetentionScope>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.Action).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Versions).WithOne().HasForeignKey(v => v.PolicyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Runs).WithOne().HasForeignKey(r => r.PolicyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Runs).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class RetentionPolicyVersionConfiguration : IEntityTypeConfiguration<RetentionPolicyVersion>
{
    public void Configure(EntityTypeBuilder<RetentionPolicyVersion> builder)
    {
        builder.ToTable("retention_policy_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<RetentionScope>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.PolicyId, x.VersionNumber }).IsUnique();
    }
}

public class RetentionRunConfiguration : IEntityTypeConfiguration<RetentionRun>
{
    public void Configure(EntityTypeBuilder<RetentionRun> builder)
    {
        builder.ToTable("retention_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionTaken).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.PolicyId, x.ExecutedOnUtc });
    }
}

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.MetricKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Condition).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<AlertCondition>(v, (JsonSerializerOptions?)null)!).HasColumnType("TEXT");
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Channels).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<AlertChannel>>(v, (JsonSerializerOptions?)null) ?? new()).HasColumnType("TEXT");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Incidents).WithOne().HasForeignKey(i => i.AlertRuleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Incidents).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Channels).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.MetricKey);
    }
}

public class AlertIncidentConfiguration : IEntityTypeConfiguration<AlertIncident>
{
    public void Configure(EntityTypeBuilder<AlertIncident> builder)
    {
        builder.ToTable("alert_incidents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Trigger).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => new { x.AlertRuleId, x.State });
    }
}
