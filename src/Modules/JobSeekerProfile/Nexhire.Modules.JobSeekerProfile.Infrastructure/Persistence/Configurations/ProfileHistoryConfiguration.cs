using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class ProfileHistoryConfiguration : IEntityTypeConfiguration<ProfileHistory>
{
    public void Configure(EntityTypeBuilder<ProfileHistory> builder)
    {
        builder.ToTable("profile_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobSeekerProfileId)
            .IsRequired();

        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey("ProfileHistoryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
