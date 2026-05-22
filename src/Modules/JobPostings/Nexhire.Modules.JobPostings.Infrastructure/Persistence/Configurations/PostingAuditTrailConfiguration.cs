using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class PostingAuditTrailConfiguration : IEntityTypeConfiguration<PostingAuditTrail>
{
    public void Configure(EntityTypeBuilder<PostingAuditTrail> builder)
    {
        builder.ToTable("posting_audit_trails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JobPostingId).IsRequired();
        builder.HasIndex(x => x.JobPostingId).IsUnique();

        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey("PostingAuditTrailId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Actor).HasConversion(JsonConversion.Converter<Core.Domain.ValueObjects.AuditActor>()).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.StatusTransition).HasConversion(JsonConversion.NullableConverter<Core.Domain.ValueObjects.StatusTransition>()).HasColumnType("jsonb");
        builder.Property(x => x.ChangedFields).HasConversion(JsonConversion.Converter<IReadOnlyCollection<string>>()).HasColumnType("jsonb");
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.HasIndex("PostingAuditTrailId", nameof(AuditEntry.OccurredOnUtc));
    }
}
