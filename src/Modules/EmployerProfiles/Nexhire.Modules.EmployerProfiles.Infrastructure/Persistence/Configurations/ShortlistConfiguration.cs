using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Configurations;

public class ShortlistConfiguration : IEntityTypeConfiguration<Shortlist>
{
    public void Configure(EntityTypeBuilder<Shortlist> builder)
    {
        builder.ToTable("shortlists");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployerProfileId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.IsDeleted)
            .IsRequired();

        builder.Property(s => s.CreatedOnUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedOnUtc)
            .IsRequired();

        builder.HasMany(s => s.Members)
            .WithOne()
            .HasForeignKey("ShortlistId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
