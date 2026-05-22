using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class JobSeekerProfileConfiguration : IEntityTypeConfiguration<Aggregates.JobSeekerProfile>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Aggregates.JobSeekerProfile> builder)
    {
        builder.ToTable("job_seeker_profiles");

        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.UserId)
            .IsRequired();

        builder.HasIndex(ep => ep.UserId)
            .IsUnique();

        builder.Property(ep => ep.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.OwnsOne(ep => ep.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.First).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            nameBuilder.Property(n => n.Last).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        });

        builder.Property(ep => ep.Email)
            .HasConversion(
                e => e.Value,
                value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(ep => ep.Email)
            .IsUnique();

        builder.Property(ep => ep.Mobile)
            .HasConversion(
                m => m.Value,
                value => MobileNumber.Create(value).Value)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ep => ep.Gender)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ep => ep.Visibility)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ep => ep.HasActiveResume)
            .IsRequired();

        builder.Property(ep => ep.CreatedOnUtc).IsRequired();
        builder.Property(ep => ep.UpdatedOnUtc).IsRequired();

        // Concurrency token mapping
        builder.Property<uint>("version_token")
            .IsConcurrencyToken();

        // Value object mappings to JSONB
        builder.Property(ep => ep.Preferences)
            .HasConversion(new ValueConverter<JobPreferences?, string>(
                v => v == null ? "{}" : JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => string.IsNullOrEmpty(v) || v == "{}" ? null! : FromDto(JsonSerializer.Deserialize<JobPreferencesDto>(v, JsonOptions)!)!
            ))
            .HasColumnType("jsonb");

        builder.Property(ep => ep.CurrentAddress)
            .HasConversion(new ValueConverter<Address?, string>(
                v => v == null ? "{}" : JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => string.IsNullOrEmpty(v) || v == "{}" ? null! : FromDto(JsonSerializer.Deserialize<AddressDto>(v, JsonOptions)!)!
            ))
            .HasColumnType("jsonb");

        builder.Property(ep => ep.PermanentAddress)
            .HasConversion(new ValueConverter<Address?, string>(
                v => v == null ? "{}" : JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => string.IsNullOrEmpty(v) || v == "{}" ? null! : FromDto(JsonSerializer.Deserialize<AddressDto>(v, JsonOptions)!)!
            ))
            .HasColumnType("jsonb");

        builder.Property(ep => ep.RecentSalary)
            .HasConversion(new ValueConverter<Money?, string>(
                v => v == null ? "{}" : JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => string.IsNullOrEmpty(v) || v == "{}" ? null! : FromDto(JsonSerializer.Deserialize<MoneyDto>(v, JsonOptions)!)!
            ))
            .HasColumnType("jsonb");

        builder.Property(ep => ep.PublicSharing)
            .HasConversion(new ValueConverter<PublicSharingSettings, string>(
                v => JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => FromDto(JsonSerializer.Deserialize<PublicSharingSettingsDto>(v, JsonOptions)!)
            ))
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(ep => ep.Verification)
            .HasConversion(new ValueConverter<VerificationFlags, string>(
                v => JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => FromDto(JsonSerializer.Deserialize<VerificationFlagsDto>(v, JsonOptions)!)
            ))
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(ep => ep.Completeness)
            .HasConversion(new ValueConverter<CompletenessScore, string>(
                v => JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => FromDto(JsonSerializer.Deserialize<CompletenessScoreDto>(v, JsonOptions)!)
            ))
            .IsRequired()
            .HasColumnType("jsonb");

        // Relationships using backing fields
        builder.HasMany(ep => ep.Education)
            .WithOne()
            .HasForeignKey("JobSeekerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Education)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(ep => ep.Experience)
            .WithOne()
            .HasForeignKey("JobSeekerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Experience)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(ep => ep.Skills)
            .WithOne()
            .HasForeignKey("JobSeekerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Skills)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(ep => ep.Documents)
            .WithOne()
            .HasForeignKey("JobSeekerProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ep => ep.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    #region JobPreferences DTO Helpers
    private class MoneyDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
    }

    private class SalaryExpectationDto
    {
        public MoneyDto Min { get; set; } = null!;
        public MoneyDto Max { get; set; } = null!;
    }

    private class JobPreferencesDto
    {
        public List<string> JobTypes { get; set; } = new();
        public List<string> Industries { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<WorkArrangement> WorkArrangements { get; set; } = new();
        public SalaryExpectationDto? SalaryExpectation { get; set; }
    }

    private static JobPreferencesDto ToDto(JobPreferences value)
    {
        return new JobPreferencesDto
        {
            JobTypes = value.JobTypes.ToList(),
            Industries = value.Industries.ToList(),
            Locations = value.Locations.ToList(),
            WorkArrangements = value.WorkArrangements.ToList(),
            SalaryExpectation = value.SalaryExpectation == null ? null : new SalaryExpectationDto
            {
                Min = new MoneyDto { Amount = value.SalaryExpectation.Min.Amount, Currency = value.SalaryExpectation.Min.Currency },
                Max = new MoneyDto { Amount = value.SalaryExpectation.Max.Amount, Currency = value.SalaryExpectation.Max.Currency }
            }
        };
    }

    private static JobPreferences? FromDto(JobPreferencesDto dto)
    {
        if (dto == null) return null;
        var salaryExpectation = dto.SalaryExpectation == null ? null : SalaryExpectation.Create(
            Money.Create(dto.SalaryExpectation.Min.Amount, dto.SalaryExpectation.Min.Currency).Value,
            Money.Create(dto.SalaryExpectation.Max.Amount, dto.SalaryExpectation.Max.Currency).Value
        ).Value;

        return JobPreferences.Create(
            dto.JobTypes,
            dto.Industries,
            dto.Locations,
            dto.WorkArrangements,
            salaryExpectation
        ).Value;
    }
    #endregion

    #region Address DTO Helpers
    private class AddressDto
    {
        public string Line1 { get; set; } = null!;
        public string? Line2 { get; set; }
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Postcode { get; set; } = null!;
        public string Country { get; set; } = null!;
    }

    private static AddressDto ToDto(Address address)
    {
        return new AddressDto
        {
            Line1 = address.Line1,
            Line2 = address.Line2,
            City = address.City,
            District = address.District,
            Postcode = address.Postcode,
            Country = address.Country
        };
    }

    private static Address? FromDto(AddressDto dto)
    {
        if (dto == null) return null;
        return Address.Create(dto.Line1, dto.Line2, dto.City, dto.District, dto.Postcode, dto.Country).Value;
    }
    #endregion

    #region Money DTO Helpers
    private static MoneyDto ToDto(Money money)
    {
        return new MoneyDto
        {
            Amount = money.Amount,
            Currency = money.Currency
        };
    }

    private static Money? FromDto(MoneyDto dto)
    {
        if (dto == null) return null;
        return Money.Create(dto.Amount, dto.Currency).Value;
    }
    #endregion

    #region PublicSharingSettings DTO Helpers
    private class FileReferenceDto
    {
        public string StorageKey { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public long SizeBytes { get; set; }
    }

    private class PublicSharingSettingsDto
    {
        public bool Enabled { get; set; }
        public string? Slug { get; set; }
        public FileReferenceDto? QrCodeRef { get; set; }
    }

    private static PublicSharingSettingsDto ToDto(PublicSharingSettings value)
    {
        return new PublicSharingSettingsDto
        {
            Enabled = value.Enabled,
            Slug = value.Slug,
            QrCodeRef = value.QrCodeRef == null ? null : new FileReferenceDto
            {
                StorageKey = value.QrCodeRef.StorageKey,
                OriginalFileName = value.QrCodeRef.OriginalFileName,
                MimeType = value.QrCodeRef.MimeType,
                SizeBytes = value.QrCodeRef.SizeBytes
            }
        };
    }

    private static PublicSharingSettings FromDto(PublicSharingSettingsDto dto)
    {
        if (dto == null) return null!;
        if (!dto.Enabled) return PublicSharingSettings.CreateDisabled().Value;

        var qrRef = FileReference.Create(dto.QrCodeRef!.StorageKey, dto.QrCodeRef.OriginalFileName, dto.QrCodeRef.MimeType, dto.QrCodeRef.SizeBytes).Value;
        return PublicSharingSettings.CreateEnabled(dto.Slug!, qrRef).Value;
    }
    #endregion

    #region VerificationFlags DTO Helpers
    private class VerificationFlagsDto
    {
        public bool IdentityVerified { get; set; }
        public bool EducationVerified { get; set; }
        public bool SelfAttested { get; set; }
    }

    private static VerificationFlagsDto ToDto(VerificationFlags value)
    {
        return new VerificationFlagsDto
        {
            IdentityVerified = value.IdentityVerified,
            EducationVerified = value.EducationVerified,
            SelfAttested = value.SelfAttested
        };
    }

    private static VerificationFlags FromDto(VerificationFlagsDto dto)
    {
        if (dto == null) return null!;
        return VerificationFlags.Create(dto.IdentityVerified, dto.EducationVerified, dto.SelfAttested).Value;
    }
    #endregion

    #region CompletenessScore DTO Helpers
    private class CompletenessScoreDto
    {
        public int Percentage { get; set; }
        public List<string> MissingSections { get; set; } = new();
    }

    private static CompletenessScoreDto ToDto(CompletenessScore value)
    {
        return new CompletenessScoreDto
        {
            Percentage = value.Percentage,
            MissingSections = value.MissingSections.ToList()
        };
    }

    private static CompletenessScore FromDto(CompletenessScoreDto dto)
    {
        if (dto == null) return null!;
        return CompletenessScore.Create(dto.Percentage, dto.MissingSections).Value;
    }
    #endregion
}
