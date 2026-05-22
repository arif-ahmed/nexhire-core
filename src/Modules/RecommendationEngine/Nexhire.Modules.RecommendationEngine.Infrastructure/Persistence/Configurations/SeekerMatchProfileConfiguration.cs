using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class SeekerMatchProfileConfiguration : IEntityTypeConfiguration<SeekerMatchProfile>
{
    public void Configure(EntityTypeBuilder<SeekerMatchProfile> builder)
    {
        builder.ToTable("seeker_match_profiles");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new SeekerMatchProfileId(v));

        builder.Property(e => e.JobSeekerId).IsRequired();
        builder.Property(e => e.EducationLevel).HasConversion<string>().IsRequired();
        builder.Property(e => e.TotalExperienceYears).IsRequired().HasDefaultValue(0m);
        builder.Property(e => e.PrivacyLevel).HasConversion<string>().HasDefaultValue(PrivacyLevel.ApplyOnly);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property<List<SkillRequirement>>("_skills")
            .HasColumnName("skills")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<SkillRequirement>>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.Property<List<string>>("_trainingCredentials")
            .HasColumnName("training_credentials")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<GeoLocation>(v, (JsonSerializerOptions?)null));

        builder.Property(e => e.SalaryExpectation)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<SalaryRange>(v, (JsonSerializerOptions?)null));

        builder.HasIndex(e => e.JobSeekerId).IsUnique();
        builder.HasIndex(e => e.PrivacyLevel);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.EducationLevel);
    }
}
