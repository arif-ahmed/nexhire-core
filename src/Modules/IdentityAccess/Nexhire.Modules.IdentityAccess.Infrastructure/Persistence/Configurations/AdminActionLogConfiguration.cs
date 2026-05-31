using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.IdentityAccess.Domain.Domain;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Configurations;

public class AdminActionLogConfiguration : IEntityTypeConfiguration<AdminActionLog>
{
    public void Configure(EntityTypeBuilder<AdminActionLog> builder)
    {
        builder.ToTable("admin_action_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        builder.Property(x => x.ActionType)
            .HasConversion(a => a.ToString(), v => Enum.Parse<AdminActionType>(v))
            .HasColumnName("action_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetUserId)
            .HasColumnName("target_user_id")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(2000);

        builder.Property(x => x.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => x.AdminUserId);
        builder.HasIndex(x => x.OccurredOnUtc);
    }
}
