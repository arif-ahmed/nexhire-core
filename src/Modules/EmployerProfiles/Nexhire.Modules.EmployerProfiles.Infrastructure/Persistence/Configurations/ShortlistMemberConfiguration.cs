using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Configurations;

public class ShortlistMemberConfiguration : IEntityTypeConfiguration<ShortlistMember>
{
    public void Configure(EntityTypeBuilder<ShortlistMember> builder)
    {
        builder.ToTable("shortlist_members");
        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.CandidateUserId)
            .IsRequired();

        builder.Property(sm => sm.MatchScore)
            .IsRequired(false);

        builder.Property(sm => sm.AddedOnUtc)
            .IsRequired();
    }
}
