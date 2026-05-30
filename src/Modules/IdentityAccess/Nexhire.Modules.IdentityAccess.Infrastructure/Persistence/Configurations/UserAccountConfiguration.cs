using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.IdentityAccess.Domain;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.OwnsOne(u => u.FullName, fullNameBuilder =>
        {
            fullNameBuilder.Property(fn => fn.FirstName)
                .HasColumnName("first_name")
                .IsRequired()
                .HasMaxLength(100);

            fullNameBuilder.Property(fn => fn.LastName)
                .HasColumnName("last_name")
                .IsRequired()
                .HasMaxLength(100);
        });

        builder.Property(u => u.CreatedAtUtc).IsRequired();
    }
}
