using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class TalentPoolConfiguration : IEntityTypeConfiguration<TalentPool>
{
    public void Configure(EntityTypeBuilder<TalentPool> builder)
    {
        builder.ToTable("talent_pools");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new TalentPoolId(v));

        builder.Property(e => e.EmployerId).IsRequired();
        builder.Property(e => e.RecruiterId).IsRequired();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.Description);
        builder.Property(e => e.IsShared).HasDefaultValue(false);

        builder.Property(e => e.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.HasMany(e => e.Members)
            .WithOne()
            .HasForeignKey("TalentPoolId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.EmployerId);
    }
}

public class TalentPoolCandidateConfiguration : IEntityTypeConfiguration<TalentPoolCandidate>
{
    public void Configure(EntityTypeBuilder<TalentPoolCandidate> builder)
    {
        builder.ToTable("talent_pool_candidates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.AddedByRecruiterId).IsRequired();
        builder.Property(e => e.Note);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.AddedAtUtc).IsRequired();
        builder.Property(e => e.RemovedAtUtc);

        builder.HasIndex("TalentPoolId", nameof(TalentPoolCandidate.JobSeekerId)).IsUnique();
    }
}
