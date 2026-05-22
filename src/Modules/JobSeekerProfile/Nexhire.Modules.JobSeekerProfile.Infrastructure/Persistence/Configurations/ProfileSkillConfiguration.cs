using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class ProfileSkillConfiguration : IEntityTypeConfiguration<ProfileSkill>
{
    public void Configure(EntityTypeBuilder<ProfileSkill> builder)
    {
        builder.ToTable("profile_skills");

        builder.HasKey(x => x.Id);

        builder.OwnsOne(s => s.CanonicalSkillRef, skillRefBuilder =>
        {
            skillRefBuilder.Property(sr => sr.TaxonomyCode).HasColumnName("taxonomy_code").HasMaxLength(100).IsRequired();
            skillRefBuilder.Property(sr => sr.DisplayLabel).HasColumnName("display_label").HasMaxLength(200).IsRequired();
        });

        builder.Property(x => x.RawLabel)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Proficiency)
            .IsRequired();
    }
}
