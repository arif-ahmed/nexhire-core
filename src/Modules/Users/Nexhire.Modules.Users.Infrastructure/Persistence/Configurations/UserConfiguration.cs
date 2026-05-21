using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.Users.Core.Domain;
using Nexhire.Modules.Users.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Users.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        // Map Email Value Object as a simple string column
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email).IsUnique();

        // Map FullName Value Object inline
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
