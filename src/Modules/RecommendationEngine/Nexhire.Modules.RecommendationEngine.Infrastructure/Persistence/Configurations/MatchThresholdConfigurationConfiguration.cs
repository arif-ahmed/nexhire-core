using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class MatchThresholdConfigurationConfiguration : IEntityTypeConfiguration<MatchThresholdConfiguration>
{
    public void Configure(EntityTypeBuilder<MatchThresholdConfiguration> builder)
    {
        builder.ToTable("match_threshold_configurations");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new MatchThresholdConfigurationId(v));

        builder.Property(e => e.GlobalThresholdPercent).HasDefaultValue(60);

        builder.Property<List<ThresholdChangeEntry>>("_changeLog")
            .HasColumnName("change_log")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<ThresholdChangeEntry>>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();
    }
}
