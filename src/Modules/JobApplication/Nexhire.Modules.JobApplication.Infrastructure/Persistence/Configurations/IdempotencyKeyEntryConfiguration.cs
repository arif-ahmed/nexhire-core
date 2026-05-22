using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyKeyEntryConfiguration : IEntityTypeConfiguration<IdempotencyKeyEntry>
{
    public void Configure(EntityTypeBuilder<IdempotencyKeyEntry> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(x => x.Key);
        
        builder.Property(x => x.ApplicationId)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
