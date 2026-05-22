using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Configurations;

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("applications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => new ApplicationId(v));

        builder.Property(x => x.JobPostingId).IsRequired();
        builder.Property(x => x.JobSeekerId).IsRequired();
        builder.Property(x => x.EmployerId).IsRequired();
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CandidateSnapshot)
            .HasConversion(JsonConversion.Converter<CandidateSnapshot>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ResumeDocumentId).IsRequired();

        builder.Property(x => x.CoverLetter)
            .HasConversion(v => v == null ? null : v.Text, v => string.IsNullOrWhiteSpace(v) ? null : CoverLetter.Create(v).Value)
            .HasMaxLength(4000);

        builder.Property(x => x.MatchScoreAtApply);

        builder.Property(x => x.ReplacesApplicationId)
            .HasConversion(v => v == null ? (Guid?)null : v.Value, v => v == null ? null : new ApplicationId(v.Value));

        builder.Property(x => x.IdempotencyKey).IsRequired();
        
        builder.Property(x => x.AppliedOnUtc).IsRequired();
        builder.Property(x => x.LastStatusChangeOnUtc).IsRequired();
        builder.Property(x => x.WithdrawnOnUtc);
        builder.Property(x => x.HiredOnUtc);
        builder.Property(x => x.RejectedOnUtc);

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.JobSeekerId);
        builder.HasIndex(x => x.JobPostingId);
        builder.HasIndex(x => x.EmployerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();

        // Partial unique index for active non-terminal applications
        builder.HasIndex(x => new { x.JobSeekerId, x.JobPostingId })
            .HasDatabaseName("IX_applications_seeker_posting_active")
            .IsUnique()
            .HasFilter("status IN ('Submitted', 'UnderReview', 'Shortlisted', 'Interview', 'Offered')");

        // Owned entity collection for Stages
        builder.OwnsMany(x => x.Stages, stageBuilder =>
        {
            stageBuilder.ToTable("application_stages");
            stageBuilder.WithOwner().HasForeignKey("ApplicationId");
            stageBuilder.HasKey(x => x.Id);
            stageBuilder.Property(x => x.Id)
                .ValueGeneratedNever();
            
            stageBuilder.Property(x => x.Stage)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            stageBuilder.Property(x => x.EnteredOnUtc).IsRequired();

            stageBuilder.Property(x => x.EnteredByRole)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            stageBuilder.Property(x => x.EnteredByUserId);
            stageBuilder.Property(x => x.ReasonCode).HasMaxLength(100);
            stageBuilder.Property(x => x.Comment).HasMaxLength(1000);
        });
    }
}
