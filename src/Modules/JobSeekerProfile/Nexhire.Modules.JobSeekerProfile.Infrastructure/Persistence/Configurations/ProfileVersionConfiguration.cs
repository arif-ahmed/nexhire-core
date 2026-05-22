using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Configurations;

public class ProfileVersionConfiguration : IEntityTypeConfiguration<ProfileVersion>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<ProfileVersion> builder)
    {
        builder.ToTable("profile_version");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.ChangedFields)
            .HasConversion(new ValueConverter<IReadOnlyCollection<string>, string>(
                v => JsonSerializer.Serialize(v ?? new List<string>(), JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
            ))
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Navigation(x => x.ChangedFields)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();
    }
}
