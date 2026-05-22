using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class JobRecommendationSetConfiguration : IEntityTypeConfiguration<JobRecommendationSet>
{
    public void Configure(EntityTypeBuilder<JobRecommendationSet> builder)
    {
        builder.ToTable("job_recommendation_sets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new JobRecommendationSetId(v));

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.ComputedAtUtc).IsRequired();

        builder.HasMany(e => e.Recommendations)
            .WithOne()
            .HasForeignKey("JobRecommendationSetId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Recommendations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.JobSeekerId, e.ComputedAtUtc }).IsDescending(false, true);
    }
}

public class RecommendedJobConfiguration : IEntityTypeConfiguration<RecommendedJob>
{
    public void Configure(EntityTypeBuilder<RecommendedJob> builder)
    {
        builder.ToTable("recommended_jobs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobPostingId).IsRequired();
        builder.Property(e => e.MatchScore).IsRequired();
        builder.Property(e => e.HybridScore).IsRequired();
        builder.Property(e => e.IsSuppressed).IsRequired();

        builder.Property(e => e.Reason)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<RecommendationReason>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();
    }
}
