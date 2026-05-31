using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.IdentityAccess.Domain.Domain;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Configurations;

public class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("otp_challenges");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OtpChallengeId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.UserAccountId)
            .HasConversion(id => id.Value, value => new UserAccountId(value))
            .HasColumnName("user_account_id")
            .IsRequired();

        builder.Property(x => x.Purpose)
            .HasConversion(p => p.ToString(), v => Enum.Parse<OtpPurpose>(v))
            .HasColumnName("purpose")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(s => s.ToString(), v => Enum.Parse<OtpStatus>(v))
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CodeHash).HasColumnName("code_hash").IsRequired();
        builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc").IsRequired();
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(x => x.MaxAttempts).HasColumnName("max_attempts").IsRequired();
        builder.Property(x => x.IssuedOnUtc).HasColumnName("issued_on_utc").IsRequired();
        builder.Property(x => x.VerifiedOnUtc).HasColumnName("verified_on_utc");
        
        builder.Property<byte[]>("Version").IsRowVersion().HasColumnName("version");

        builder.HasIndex(x => new { x.UserAccountId, x.Purpose })
            .HasFilter("status = 'Issued'");
    }
}
