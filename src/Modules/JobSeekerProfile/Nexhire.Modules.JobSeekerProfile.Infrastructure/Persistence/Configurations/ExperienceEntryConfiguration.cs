using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class ExperienceEntryConfiguration : IEntityTypeConfiguration<ExperienceEntry>
{
    public void Configure(EntityTypeBuilder<ExperienceEntry> builder)
    {
        builder.ToTable("experience_entry");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Company)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(e => e.Period, periodBuilder =>
        {
            periodBuilder.Property(p => p.Start).HasColumnName("period_start").IsRequired();
            periodBuilder.Property(p => p.End).HasColumnName("period_end");
        });

        builder.Property(x => x.IsCurrent)
            .IsRequired();

        builder.Property(x => x.Responsibilities)
            .HasMaxLength(2000);
    }
}
