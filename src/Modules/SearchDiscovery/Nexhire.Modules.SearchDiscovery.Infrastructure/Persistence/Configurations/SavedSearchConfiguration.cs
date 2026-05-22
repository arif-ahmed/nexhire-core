using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Configurations;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("saved_searches");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeekerUserId).IsRequired();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.NotificationPreference).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.IsDeleted).IsRequired();
        builder.Property(s => s.CreatedOnUtc).IsRequired();
        builder.Property(s => s.UpdatedOnUtc).IsRequired();

        builder.Property(s => s.Criteria)
            .HasColumnType("jsonb")
            .HasConversion(
                c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<SearchCriteria>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.HasIndex(s => s.SeekerUserId);
        builder.HasIndex(s => new { s.SeekerUserId, s.Name }).IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
