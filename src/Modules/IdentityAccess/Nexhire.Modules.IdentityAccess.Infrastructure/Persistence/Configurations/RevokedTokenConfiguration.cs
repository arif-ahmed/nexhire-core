using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.IdentityAccess.Domain.Domain;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Configurations;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("revoked_tokens");

        builder.HasKey(x => x.TokenIdOrRefreshHash);
        builder.Property(x => x.TokenIdOrRefreshHash)
            .HasColumnName("token_id_or_refresh_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.RevokedOnUtc).HasColumnName("revoked_on_utc").IsRequired();
        builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc").IsRequired();

        builder.HasIndex(x => x.ExpiresOnUtc);
    }
}
