using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class MatchingWeightProfileConfiguration : IEntityTypeConfiguration<MatchingWeightProfile>
{
    public void Configure(EntityTypeBuilder<MatchingWeightProfile> builder)
    {
        builder.ToTable("matching_weight_profiles");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new MatchingWeightProfileId(v));

        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.VariantId).IsRequired().HasDefaultValue("control");
        builder.Property(e => e.VariantAllocationPercent).HasDefaultValue(100);
        builder.Property(e => e.IsActive).HasDefaultValue(false);
        builder.Property(e => e.CreatedBy).IsRequired();
        builder.Property(e => e.SupersededByVersion);

        builder.Property(e => e.Weights)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<FactorWeights>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.HasIndex(e => e.Version).IsUnique();
        builder.HasIndex(e => e.VariantId)
            .HasFilter("\"IsActive\" = true")
            .IsDescending();
    }
}
