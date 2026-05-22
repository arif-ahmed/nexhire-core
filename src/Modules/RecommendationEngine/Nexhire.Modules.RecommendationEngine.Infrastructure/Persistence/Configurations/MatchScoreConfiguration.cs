using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class MatchScoreConfiguration : IEntityTypeConfiguration<MatchScore>
{
    public void Configure(EntityTypeBuilder<MatchScore> builder)
    {
        builder.ToTable("match_scores");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new MatchScoreId(v));

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.JobPostingId).IsRequired();
        builder.Property(e => e.OverallScore).IsRequired();
        builder.Property(e => e.WeightProfileVersion).IsRequired();
        builder.Property(e => e.WeightVariantId);
        builder.Property(e => e.IsStale).HasDefaultValue(false);

        builder.Property(e => e.Breakdown)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MatchBreakdown>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.Property(e => e.Strengths)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(f => f.ToString()).ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!.Select(s => Enum.Parse<MatchFactor>(s)).ToList())
            .IsRequired();

        builder.Property(e => e.Gaps)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(f => f.ToString()).ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!.Select(s => Enum.Parse<MatchFactor>(s)).ToList())
            .IsRequired();

        builder.HasIndex(e => new { e.JobSeekerId, e.JobPostingId }).IsUnique();
        builder.HasIndex(e => new { e.JobPostingId, e.OverallScore }).IsDescending(false, true);
        builder.HasIndex(e => new { e.JobSeekerId, e.OverallScore }).IsDescending(false, true);
        builder.HasIndex(e => e.IsStale);
    }
}
