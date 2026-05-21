using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Configurations;

public class SupplementaryDocumentConfiguration : IEntityTypeConfiguration<SupplementaryDocument>
{
    public void Configure(EntityTypeBuilder<SupplementaryDocument> builder)
    {
        builder.ToTable("supplementary_documents");
        builder.HasKey(sd => sd.Id);

        builder.Property(sd => sd.Kind)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(sd => sd.File, fileBuilder =>
        {
            fileBuilder.Property(fr => fr.StorageKey).HasColumnName("file_storage_key").HasMaxLength(500).IsRequired();
            fileBuilder.Property(fr => fr.OriginalFileName).HasColumnName("file_original_file_name").HasMaxLength(255).IsRequired();
            fileBuilder.Property(fr => fr.MimeType).HasColumnName("file_mime_type").HasMaxLength(100).IsRequired();
            fileBuilder.Property(fr => fr.SizeBytes).HasColumnName("file_size_bytes").IsRequired();
        });

        builder.OwnsOne(sd => sd.ScanResult, scanBuilder =>
        {
            scanBuilder.Property(sr => sr.Status).HasColumnName("scan_status").HasConversion<string>().HasMaxLength(50).IsRequired();
            scanBuilder.Property(sr => sr.ScannedOnUtc).HasColumnName("scanned_on_utc");
        });

        builder.Property(sd => sd.UploadedOnUtc).IsRequired();
    }
}
