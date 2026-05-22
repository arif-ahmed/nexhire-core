using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using System.Text.Json;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Configurations;

public class EmbeddingRecordConfiguration : IEntityTypeConfiguration<EmbeddingRecord>
{
    public void Configure(EntityTypeBuilder<EmbeddingRecord> builder)
    {
        builder.ToTable("embedding_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new EmbeddingRecordId(v));

        builder.Property(e => e.OwnerId).IsRequired();
        builder.Property(e => e.OwnerType).HasConversion<string>().IsRequired();

        builder.Property(e => e.Vector)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<EmbeddingVector>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        builder.Property(e => e.ModelVersion).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasIndex(e => new { e.OwnerType, e.OwnerId });
    }
}
