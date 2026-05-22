using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class EducationEntryConfiguration : IEntityTypeConfiguration<EducationEntry>
{
    public void Configure(EntityTypeBuilder<EducationEntry> builder)
    {
        builder.ToTable("education_entry");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Degree)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Institution)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(e => e.Period, periodBuilder =>
        {
            periodBuilder.Property(p => p.Start).HasColumnName("period_start").IsRequired();
            periodBuilder.Property(p => p.End).HasColumnName("period_end");
        });

        builder.Property(x => x.Gpa)
            .HasPrecision(3, 2);
    }
}
