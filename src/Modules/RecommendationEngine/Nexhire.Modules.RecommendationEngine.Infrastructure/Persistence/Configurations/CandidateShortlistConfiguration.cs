using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class CandidateShortlistConfiguration : IEntityTypeConfiguration<CandidateShortlist>
{
    public void Configure(EntityTypeBuilder<CandidateShortlist> builder)
    {
        builder.ToTable("candidate_shortlists");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new CandidateShortlistId(v));

        builder.Property(e => e.JobPostingId).IsRequired();
        builder.Property(e => e.ConfiguredSize).HasDefaultValue(100);
        builder.Property(e => e.RefreshState).HasConversion<string>().HasDefaultValue(ShortlistRefreshState.Fresh);
        builder.Property(e => e.LastRefreshedUtc).IsRequired();

        builder.HasMany(e => e.Candidates)
            .WithOne()
            .HasForeignKey("CandidateShortlistId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Candidates)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.JobPostingId).IsUnique();
    }
}

public class ShortlistCandidateConfiguration : IEntityTypeConfiguration<ShortlistCandidate>
{
    public void Configure(EntityTypeBuilder<ShortlistCandidate> builder)
    {
        builder.ToTable("shortlist_candidates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.OverallMatchScore).IsRequired();
        builder.Property(e => e.InclusionReason).HasConversion<string>().IsRequired();
        builder.Property(e => e.AppliedAtUtc);

        builder.Property(e => e.FitAnalysis)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<FitAnalysis>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();
    }
}
