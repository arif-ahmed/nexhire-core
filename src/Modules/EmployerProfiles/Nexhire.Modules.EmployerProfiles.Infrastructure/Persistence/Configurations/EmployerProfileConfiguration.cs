using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Configurations;

public class EmployerProfileConfiguration : IEntityTypeConfiguration<EmployerProfile>
{
    public void Configure(EntityTypeBuilder<EmployerProfile> builder)
    {
        builder.ToTable("employer_profiles");

        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.UserId)
            .IsRequired();

        builder.HasIndex(ep => ep.UserId)
            .IsUnique();

        builder.Property(ep => ep.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ep => ep.CompanyName)
            .HasConversion(
                cn => cn.Value,
                value => CompanyName.Create(value).Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ep => ep.Email)
            .HasConversion(
                e => e.Value,
                value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ep => ep.Mobile)
            .HasConversion(
                m => m.Value,
                value => MobileNumber.Create(value).Value)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ep => ep.CompanyIdentifier)
            .HasConversion(
                ci => ci.Value,
                value => CompanyIdentifier.Create(value).Value)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(ep => ep.CompanyIdentifier)
            .IsUnique();

        builder.Property(ep => ep.Website)
            .HasConversion(
                w => w == null ? null : w.Value,
                value => value == null ? null : WebsiteUrl.Create(value).Value)
            .HasMaxLength(2000);

        builder.Property(ep => ep.Industry)
            .HasMaxLength(200);

        builder.Property(ep => ep.CompanySize)
            .HasConversion(
                cs => cs == null ? null : cs.Value.ToString(),
                value => value == null ? null : CompanySize.Create(value).Value)
            .HasMaxLength(50);

        builder.Property(ep => ep.Description)
            .HasConversion(
                d => d == null ? null : d.Value,
                value => value == null ? null : CompanyDescription.Create(value).Value)
            .HasMaxLength(5000);

        builder.OwnsOne(ep => ep.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Line1).HasColumnName("address_line1").HasMaxLength(200);
            addressBuilder.Property(a => a.Line2).HasColumnName("address_line2").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100);
            addressBuilder.Property(a => a.District).HasColumnName("address_district").HasMaxLength(100);
            addressBuilder.Property(a => a.Postcode).HasColumnName("address_postcode").HasMaxLength(50);
            addressBuilder.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(100);
        });

        builder.OwnsOne(ep => ep.Logo, logoBuilder =>
        {
            logoBuilder.Property(fr => fr.StorageKey).HasColumnName("logo_storage_key").HasMaxLength(500);
            logoBuilder.Property(fr => fr.OriginalFileName).HasColumnName("logo_original_file_name").HasMaxLength(255);
            logoBuilder.Property(fr => fr.MimeType).HasColumnName("logo_mime_type").HasMaxLength(100);
            logoBuilder.Property(fr => fr.SizeBytes).HasColumnName("logo_size_bytes");
        });

        builder.OwnsOne(ep => ep.Verification, vBuilder =>
        {
            vBuilder.Property(v => v.Outcome).HasColumnName("verification_outcome").HasConversion<string>().HasMaxLength(50);
            vBuilder.Property(v => v.Method).HasColumnName("verification_method").HasConversion<string>().HasMaxLength(50);
            vBuilder.Property(v => v.EvidenceRef).HasColumnName("verification_evidence_ref").HasMaxLength(500);
            vBuilder.Property(v => v.RejectionReason).HasColumnName("verification_rejection_reason").HasMaxLength(1000);
            vBuilder.Property(v => v.LastAttemptUtc).HasColumnName("verification_last_attempt_utc");
        });

        builder.OwnsOne(ep => ep.Completeness, cBuilder =>
        {
            cBuilder.Property(c => c.Level1Complete).HasColumnName("completeness_level1_complete");
            cBuilder.Property(c => c.Level2Complete).HasColumnName("completeness_level2_complete");
        });

        builder.Property(ep => ep.StatusBeforeSuspend)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ep => ep.CreatedOnUtc).IsRequired();
        builder.Property(ep => ep.UpdatedOnUtc).IsRequired();

        // Navigations using backing fields
        builder.HasMany(ep => ep.Images)
            .WithOne()
            .HasForeignKey("EmployerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(ep => ep.Documents)
            .WithOne()
            .HasForeignKey("EmployerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
