using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserAccountId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.OwnsOne(x => x.Credential, c =>
        {
            c.Property(p => p.Email)
                .HasConversion(e => e.Value, v => EmailAddress.Create(v).Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            c.Property(p => p.Mobile)
                .HasConversion(
                    m => m != null ? m.Value : null,
                    v => v != null ? MobileNumber.Create(v, "+880").Value : null)
                .HasColumnName("mobile")
                .HasMaxLength(20);

            c.OwnsOne(p => p.PasswordHash, ph =>
            {
                ph.Property(h => h.Algorithm).HasColumnName("password_algorithm").HasMaxLength(20).IsRequired();
                ph.Property(h => h.Value).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            });

            c.HasIndex(p => p.Email).IsUnique();
            c.HasIndex(p => p.Mobile).IsUnique();
        });

        builder.Property(x => x.Role)
            .HasConversion(r => r.ToString(), v => Enum.Parse<UserRole>(v))
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(s => s.ToString(), v => Enum.Parse<AccountStatus>(v))
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsOne(x => x.LockState, ls =>
        {
            ls.ToJson("lock_state");
        });

        builder.OwnsOne(x => x.Mfa, mfa =>
        {
            mfa.ToJson("mfa");
        });

        builder.Property(x => x.PasswordHistory)
            .HasColumnName("password_history")
            .HasColumnType("jsonb");

        builder.Property(x => x.SuspendedReason).HasColumnName("suspended_reason").HasMaxLength(1000);
        builder.Property(x => x.ActivatedOnUtc).HasColumnName("activated_on_utc");
        builder.Property(x => x.DeactivatedOnUtc).HasColumnName("deactivated_on_utc");
        builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        builder.Property(x => x.UpdatedOnUtc).HasColumnName("updated_on_utc").IsRequired();
        
        builder.Property<byte[]>("Version").IsRowVersion().HasColumnName("version");

        // Indices
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Role);

        // Owned collections
        builder.OwnsMany(x => x.Sessions, s =>
        {
            s.ToTable("sessions");
            s.WithOwner().HasForeignKey("user_account_id");
            s.HasKey(x => x.Id);
            s.Property(x => x.Id).HasConversion(id => id.Value, g => new SessionId(g));
            
            s.Property(x => x.UserAccountId).HasConversion(id => id.Value, v => new UserAccountId(v)).HasColumnName("user_account_id_ref");
            s.Property(x => x.Channel).HasConversion(c => c.ToString(), v => Enum.Parse<Channel>(v)).HasColumnName("channel");
            s.Property(x => x.DeviceFingerprint).HasConversion(d => d.Value, v => DeviceFingerprint.Create(v).Value).HasColumnName("device_fingerprint");
            s.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash");
            s.Property(x => x.IssuedOnUtc).HasColumnName("issued_on_utc");
            s.Property(x => x.LastSeenUtc).HasColumnName("last_seen_utc");
            s.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc");
            s.Property(x => x.RevokedOnUtc).HasColumnName("revoked_on_utc");
        });

        builder.OwnsMany(x => x.BackupCodes, b =>
        {
            b.ToTable("backup_codes");
            b.WithOwner().HasForeignKey("user_account_id");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, g => new BackupCodeId(g));
            b.Property(x => x.CodeHash).HasColumnName("code_hash");
            b.Property(x => x.UsedOnUtc).HasColumnName("used_on_utc");
        });

        builder.OwnsMany(x => x.TrustedDevices, td =>
        {
            td.ToTable("trusted_devices");
            td.WithOwner().HasForeignKey("user_account_id");
            td.HasKey(x => x.Id);
            td.Property(x => x.Id).HasConversion(id => id.Value, g => new TrustedDeviceId(g));
            td.Property(x => x.DeviceFingerprint).HasConversion(d => d.Value, v => DeviceFingerprint.Create(v).Value).HasColumnName("device_fingerprint");
            td.Property(x => x.Label).HasColumnName("label");
            td.Property(x => x.TrustedUntilUtc).HasColumnName("trusted_until_utc");
        });

        builder.OwnsMany(x => x.PasswordResetTokens, prt =>
        {
            prt.ToTable("password_reset_tokens");
            prt.WithOwner().HasForeignKey("user_account_id");
            prt.HasKey(x => x.Id);
            prt.Property(x => x.Id).HasConversion(id => id.Value, g => new PasswordResetTokenId(g));
            prt.Property(x => x.TokenHash).HasColumnName("token_hash");
            prt.Property(x => x.IssuedOnUtc).HasColumnName("issued_on_utc");
            prt.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc");
            prt.Property(x => x.UsedOnUtc).HasColumnName("used_on_utc");
        });
    }
}
