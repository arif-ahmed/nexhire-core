using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Configurations;

public class JobIndexEntryConfiguration : IEntityTypeConfiguration<JobIndexEntry>
{
    public void Configure(EntityTypeBuilder<JobIndexEntry> builder)
    {
        builder.ToTable("job_index_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Summary).HasMaxLength(2000);
        builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Skills)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(e => e.EducationRequirement).HasMaxLength(200);
        builder.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.WorkFormat).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.SalaryMin).HasPrecision(18, 2);
        builder.Property(e => e.SalaryMax).HasPrecision(18, 2);
        builder.Property(e => e.SalaryCurrency).HasMaxLength(10);
        builder.Property(e => e.SectorIndustry).HasMaxLength(200);
        builder.Property(e => e.SourcePostingVersion).IsRequired();
        builder.Property(e => e.IndexedOnUtc).IsRequired();
        builder.Property(e => e.UpdatedOnUtc).IsRequired();

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.District).HasColumnName("location_district").IsRequired().HasMaxLength(200);
            loc.Property(l => l.City).HasColumnName("location_city").HasMaxLength(200);
            loc.Property(l => l.Latitude).HasColumnName("location_latitude");
            loc.Property(l => l.Longitude).HasColumnName("location_longitude");
        });

        builder.HasIndex(e => e.Title);
        builder.HasIndex(e => e.PostedOnUtc).IsDescending();
        builder.HasIndex(e => e.EmploymentType);
        builder.HasIndex(e => e.ApplicationDeadlineUtc);
    }
}
