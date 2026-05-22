using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Configurations;

public class FavoriteJobConfiguration : IEntityTypeConfiguration<FavoriteJob>
{
    public void Configure(EntityTypeBuilder<FavoriteJob> builder)
    {
        builder.ToTable("favorite_jobs");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.SeekerUserId).IsRequired();
        builder.Property(f => f.PostingId).IsRequired();
        builder.Property(f => f.FavoritedOnUtc).IsRequired();

        builder.HasIndex(f => f.SeekerUserId);
        builder.HasIndex(f => new { f.SeekerUserId, f.PostingId }).IsUnique();
    }
}
