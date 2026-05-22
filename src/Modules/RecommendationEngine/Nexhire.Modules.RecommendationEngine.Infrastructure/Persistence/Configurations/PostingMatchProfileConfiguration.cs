using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class PostingMatchProfileConfiguration : IEntityTypeConfiguration<PostingMatchProfile>
{
    public void Configure(EntityTypeBuilder<PostingMatchProfile> builder)
    {
        builder.ToTable("posting_match_profiles");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new PostingMatchProfileId(v));

        builder.Property(e => e.JobPostingId).IsRequired();
        builder.Property(e => e.EmployerId).IsRequired();
        builder.Property(e => e.RequiredEducationLevel).HasConversion<string>().IsRequired();
        builder.Property(e => e.RequiredExperienceYears).HasDefaultValue(0m);
        builder.Property(e => e.Status).HasConversion<string>().HasDefaultValue(PostingMatchStatus.Active);
        builder.Property(e => e.PerPostingThresholdOverride);
        builder.Property(e => e.NlpStatus).HasConversion<string>().HasDefaultValue(NlpExtractionStatus.Pending);

        builder.Property<List<SkillRequirement>>("_requiredSkills")
            .HasColumnName("required_skills")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<SkillRequirement>>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<GeoLocation>(v, (JsonSerializerOptions?)null));

        builder.Property(e => e.SalaryRange)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<SalaryRange>(v, (JsonSerializerOptions?)null));

        builder.HasIndex(e => e.JobPostingId).IsUnique();
        builder.HasIndex(e => e.EmployerId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.NlpStatus);
    }
}
