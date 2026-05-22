using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobApplication.Core.Domain;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Configurations;

public sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("bookmarks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => new BookmarkId(v));

        builder.Property(x => x.JobSeekerId).IsRequired();
        builder.Property(x => x.JobPostingId).IsRequired();
        builder.Property(x => x.BookmarkedOnUtc).IsRequired();

        // Unique composite index
        builder.HasIndex(x => new { x.JobSeekerId, x.JobPostingId })
            .IsUnique();

        builder.HasIndex(x => x.JobSeekerId);
        builder.HasIndex(x => x.JobPostingId);
    }
}
