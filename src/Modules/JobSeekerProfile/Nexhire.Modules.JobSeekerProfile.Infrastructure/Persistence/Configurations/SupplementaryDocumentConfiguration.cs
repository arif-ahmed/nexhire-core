using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class SupplementaryDocumentConfiguration : IEntityTypeConfiguration<SupplementaryDocument>
{
    public void Configure(EntityTypeBuilder<SupplementaryDocument> builder)
    {
        builder.ToTable("supp_document");

        builder.HasKey(x => x.Id);

        builder.OwnsOne(x => x.File, fileBuilder =>
        {
            fileBuilder.Property(f => f.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
            fileBuilder.Property(f => f.OriginalFileName).HasColumnName("orig_file_name").HasMaxLength(255).IsRequired();
            fileBuilder.Property(f => f.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired();
            fileBuilder.Property(f => f.SizeBytes).HasColumnName("size_bytes").IsRequired();
        });

        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.OwnsOne(x => x.ScanResult, scanBuilder =>
        {
            scanBuilder.Property(s => s.Status).HasColumnName("scan_status").HasConversion<string>().HasMaxLength(50).IsRequired();
            scanBuilder.Property(s => s.ScannedOnUtc).HasColumnName("scanned_on_utc");
        });

        builder.Property(x => x.UploadedOnUtc)
            .IsRequired();
    }
}
