using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("resumes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfileId)
            .IsRequired();

        builder.OwnsOne(x => x.File, fileBuilder =>
        {
            fileBuilder.Property(f => f.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
            fileBuilder.Property(f => f.OriginalFileName).HasColumnName("orig_file_name").HasMaxLength(255).IsRequired();
            fileBuilder.Property(f => f.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired();
            fileBuilder.Property(f => f.SizeBytes).HasColumnName("size_bytes").IsRequired();
        });

        builder.OwnsOne(x => x.ScanResult, scanBuilder =>
        {
            scanBuilder.Property(s => s.Status).HasColumnName("scan_status").HasConversion<string>().HasMaxLength(50).IsRequired();
            scanBuilder.Property(s => s.ScannedOnUtc).HasColumnName("scanned_on_utc");
        });

        builder.Property(x => x.ParseStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ParserName)
            .HasMaxLength(100);

        builder.Property(x => x.ParsedOnUtc);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.IsSuperseded)
            .IsRequired();

        builder.Property(x => x.UploadedOnUtc)
            .IsRequired();

        // Concurrency shadow token
        builder.Property<uint>("version_token")
            .IsConcurrencyToken();

        // Map ParsedResumeData as jsonb using custom DTO converters
        builder.Property(x => x.ParsedData)
            .HasConversion(new ValueConverter<ParsedResumeData?, string>(
                v => v == null ? "{}" : JsonSerializer.Serialize(ToDto(v), JsonOptions),
                v => string.IsNullOrEmpty(v) || v == "{}" ? null : FromDto(JsonSerializer.Deserialize<ParsedResumeDataDto>(v, JsonOptions)!)
            ))
            .HasColumnType("jsonb");

        // Map backing private list _mergedFieldKeys
        builder.Property(x => x.MergedFieldKeys)
            .HasConversion(new ValueConverter<IReadOnlyCollection<string>, string>(
                v => JsonSerializer.Serialize(v ?? new List<string>(), JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
            ))
            .HasColumnType("jsonb");

        builder.Navigation(x => x.MergedFieldKeys)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    #region ParsedResumeData DTO Mappings
    private class ConfidenceScoreDto
    {
        public int Value { get; set; }
    }

    private class ParsedPersonalDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public ConfidenceScoreDto Confidence { get; set; } = null!;
    }

    private class ParsedEducationDto
    {
        public string Degree { get; set; } = null!;
        public string Institution { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ConfidenceScoreDto Confidence { get; set; } = null!;
    }

    private class ParsedExperienceDto
    {
        public string Company { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string Responsibilities { get; set; } = null!;
        public ConfidenceScoreDto Confidence { get; set; } = null!;
    }

    private class ParsedSkillDto
    {
        public string RawLabel { get; set; } = null!;
        public ConfidenceScoreDto Confidence { get; set; } = null!;
    }

    private class ParsedResumeDataDto
    {
        public ParsedPersonalDto Personal { get; set; } = null!;
        public List<ParsedEducationDto> Education { get; set; } = new();
        public List<ParsedExperienceDto> Experience { get; set; } = new();
        public List<ParsedSkillDto> Skills { get; set; } = new();
    }

    private static ParsedResumeDataDto ToDto(ParsedResumeData value)
    {
        return new ParsedResumeDataDto
        {
            Personal = new ParsedPersonalDto
            {
                FirstName = value.Personal.FirstName,
                LastName = value.Personal.LastName,
                Email = value.Personal.Email,
                Mobile = value.Personal.Mobile,
                Confidence = new ConfidenceScoreDto { Value = value.Personal.Confidence.Value }
            },
            Education = value.Education.Select(e => new ParsedEducationDto
            {
                Degree = e.Degree,
                Institution = e.Institution,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Confidence = new ConfidenceScoreDto { Value = e.Confidence.Value }
            }).ToList(),
            Experience = value.Experience.Select(e => new ParsedExperienceDto
            {
                Company = e.Company,
                Role = e.Role,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsCurrent = e.IsCurrent,
                Responsibilities = e.Responsibilities,
                Confidence = new ConfidenceScoreDto { Value = e.Confidence.Value }
            }).ToList(),
            Skills = value.Skills.Select(s => new ParsedSkillDto
            {
                RawLabel = s.RawLabel,
                Confidence = new ConfidenceScoreDto { Value = s.Confidence.Value }
            }).ToList()
        };
    }

    private static ParsedResumeData FromDto(ParsedResumeDataDto dto)
    {
        if (dto == null) return null!;

        var personal = new ParsedPersonal(
            dto.Personal.FirstName,
            dto.Personal.LastName,
            dto.Personal.Email,
            dto.Personal.Mobile,
            ConfidenceScore.Create(dto.Personal.Confidence.Value).Value
        );

        var education = dto.Education.Select(e => new ParsedEducation(
            e.Degree,
            e.Institution,
            e.StartDate,
            e.EndDate,
            ConfidenceScore.Create(e.Confidence.Value).Value
        )).ToList();

        var experience = dto.Experience.Select(e => new ParsedExperience(
            e.Company,
            e.Role,
            e.StartDate,
            e.EndDate,
            e.IsCurrent,
            e.Responsibilities,
            ConfidenceScore.Create(e.Confidence.Value).Value
        )).ToList();

        var skills = dto.Skills.Select(s => new ParsedSkill(
            s.RawLabel,
            ConfidenceScore.Create(s.Confidence.Value).Value
        )).ToList();

        return ParsedResumeData.Create(personal, education, experience, skills).Value;
    }
    #endregion
}
