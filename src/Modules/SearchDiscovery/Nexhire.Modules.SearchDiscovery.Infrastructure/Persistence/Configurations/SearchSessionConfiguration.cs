using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Configurations;

public class SearchSessionConfiguration : IEntityTypeConfiguration<SearchSession>
{
    public void Configure(EntityTypeBuilder<SearchSession> builder)
    {
        builder.ToTable("search_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeekerUserId).IsRequired();
        builder.Property(s => s.ExpiresOnUtc).IsRequired();
        builder.Property(s => s.UpdatedOnUtc).IsRequired();

        builder.Property(s => s.LastCriteria)
            .HasColumnType("jsonb")
            .HasConversion(
                c => c == null ? null : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<SearchCriteria>(v, (JsonSerializerOptions?)null));

        builder.Property(s => s.DismissedRecommendationPostingIds)
            .HasColumnName("dismissed_recommendation_posting_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids.ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!.AsReadOnly());

        builder.HasIndex(s => s.SeekerUserId).IsUnique();
        builder.HasIndex(s => s.ExpiresOnUtc);
    }
}
