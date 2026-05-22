using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("job_postings");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.RequiredSkills);
        builder.Ignore(x => x.RequiredLanguages);
        builder.Ignore(x => x.DeprecatedSkillCodes);
        builder.Property(x => x.EmployerId).IsRequired();
        builder.Property(x => x.PostedByUserId).IsRequired();
        builder.HasIndex(x => x.EmployerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExternalRef);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ContractType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.EducationLevel).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.WorkFormat).HasConversion<string>().HasMaxLength(40).IsRequired();

        builder.Property(x => x.Title)
            .HasConversion(v => v.Value, v => JobTitle.Create(v).Value)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasConversion(v => v.Value, v => JobSummary.Create(v).Value)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasConversion(JsonConversion.NullableConverter<EmploymentLocation>())
            .HasColumnType("jsonb");

        builder.Property<List<RequiredSkill>>("_requiredSkills")
            .HasColumnName("required_skills")
            .HasConversion(JsonConversion.Converter<List<RequiredSkill>>())
            .HasColumnType("jsonb");

        builder.Property<List<LanguageRequirement>>("_requiredLanguages")
            .HasColumnName("required_languages")
            .HasConversion(JsonConversion.Converter<List<LanguageRequirement>>())
            .HasColumnType("jsonb");

        builder.Property<List<string>>("_deprecatedSkillCodes")
            .HasColumnName("deprecated_skill_codes")
            .HasConversion(JsonConversion.Converter<List<string>>())
            .HasColumnType("jsonb");

        builder.Property(x => x.SalaryRange)
            .HasConversion(JsonConversion.NullableConverter<SalaryRange>())
            .HasColumnType("jsonb");

        builder.Property(x => x.Deadline)
            .HasConversion(JsonConversion.Converter<ApplicationDeadline>())
            .HasColumnName("deadline")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<DateTime>("deadline_date_utc")
            .HasComputedColumnSql("(deadline->>'dateUtc')::timestamp with time zone", stored: true);
        builder.HasIndex("Status", "deadline_date_utc")
            .HasDatabaseName("ix_job_postings_status_deadline_date_utc");

        builder.Property(x => x.JobLink)
            .HasConversion(v => v == null ? null : v.Url, v => string.IsNullOrWhiteSpace(v) ? null : JobPostingLink.Create(v).Value)
            .HasMaxLength(2000);

        builder.Property(x => x.Visibility)
            .HasConversion(JsonConversion.Converter<PostingVisibility>())
            .HasColumnName("visibility")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<string>("visibility_level")
            .HasComputedColumnSql("visibility->>'level'", stored: true);
        builder.HasIndex("visibility_level")
            .HasDatabaseName("ix_job_postings_visibility_level");

        builder.Property(x => x.SchemaOrg)
            .HasConversion(JsonConversion.NullableConverter<SchemaOrgJobPosting>())
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
        builder.Property(x => x.PublishedOnUtc);
        builder.Property(x => x.RenewedFromPostingId);
        builder.Property(x => x.ExternalRef).HasMaxLength(200);
        builder.Property<uint>("version_token").IsConcurrencyToken();
    }
}
