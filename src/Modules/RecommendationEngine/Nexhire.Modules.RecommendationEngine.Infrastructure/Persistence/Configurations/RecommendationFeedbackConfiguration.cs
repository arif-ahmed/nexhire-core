using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class RecommendationFeedbackConfiguration : IEntityTypeConfiguration<RecommendationFeedback>
{
    public void Configure(EntityTypeBuilder<RecommendationFeedback> builder)
    {
        builder.ToTable("recommendation_feedback");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new RecommendationFeedbackId(v));

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.JobPostingId).IsRequired();
        builder.Property(e => e.Signal).HasConversion<string>();
        builder.Property(e => e.SuppressUntilUtc);
        builder.Property(e => e.RecordedAtUtc).IsRequired();

        builder.HasIndex(e => e.JobSeekerId);
        builder.HasIndex(e => new { e.JobSeekerId, e.JobPostingId }).IsUnique();
        builder.HasIndex(e => e.SuppressUntilUtc);
    }
}
