using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Configurations;

public sealed class WithdrawalReasonLookupConfiguration : IEntityTypeConfiguration<WithdrawalReasonLookup>
{
    public void Configure(EntityTypeBuilder<WithdrawalReasonLookup> builder)
    {
        builder.ToTable("withdrawal_reasons");

        builder.HasKey(x => x.Code);
        
        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(200)
            .IsRequired();

        // Seed data
        builder.HasData(
            new WithdrawalReasonLookup { Code = "ChangedMind", Description = "Changed mind" },
            new WithdrawalReasonLookup { Code = "AcceptedAnotherOffer", Description = "Accepted another offer" },
            new WithdrawalReasonLookup { Code = "NoLongerInterested", Description = "No longer interested" },
            new WithdrawalReasonLookup { Code = "RoleNotAsExpected", Description = "Role not as expected" },
            new WithdrawalReasonLookup { Code = "AccountDeactivated", Description = "Account deactivated" }
        );
    }
}
