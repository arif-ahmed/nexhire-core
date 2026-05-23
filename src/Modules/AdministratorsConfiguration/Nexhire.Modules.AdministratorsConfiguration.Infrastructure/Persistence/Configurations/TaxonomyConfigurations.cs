using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence.Configurations;

public sealed class TaxonomyConfiguration : IEntityTypeConfiguration<Taxonomy>
{
    public void Configure(EntityTypeBuilder<Taxonomy> builder)
    {
        builder.ToTable("taxonomies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TaxonomyKind>(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Kind).IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken() // Enables optimistic concurrency check on save
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc)
            .IsRequired();

        // Owned collection mapping for terms tree
        builder.OwnsMany(x => x.Terms, termBuilder =>
        {
            termBuilder.ToTable("taxonomy_terms");

            termBuilder.HasKey(x => x.Id);

            termBuilder.WithOwner().HasForeignKey("TaxonomyId");

            termBuilder.Property(x => x.Code)
                .HasConversion(
                    v => v.Value,
                    v => TermCode.Create(v).Value)
                .HasMaxLength(64)
                .IsRequired();

            termBuilder.Property(x => x.Label)
                .HasMaxLength(200)
                .IsRequired();

            termBuilder.Property(x => x.Category)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString() : null,
                    v => !string.IsNullOrEmpty(v) ? Enum.Parse<SkillCategory>(v) : null)
                .HasMaxLength(50);

            termBuilder.Property(x => x.ParentCode)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => !string.IsNullOrEmpty(v) ? TermCode.Create(v).Value : null)
                .HasMaxLength(64);

            termBuilder.Property(x => x.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<TermStatus>(v))
                .HasMaxLength(50)
                .IsRequired();

            termBuilder.Property(x => x.ReplacedByCode)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => !string.IsNullOrEmpty(v) ? TermCode.Create(v).Value : null)
                .HasMaxLength(64);

            termBuilder.Property(x => x.UsageCount)
                .HasDefaultValue(0)
                .IsRequired();

            termBuilder.Property(x => x.CreatedOnUtc)
                .IsRequired();

            termBuilder.Property(x => x.DeprecatedOnUtc);

            // Indexes for fast lookup
            termBuilder.HasIndex("TaxonomyId");
            termBuilder.HasIndex(x => x.Code);
            termBuilder.HasIndex(x => x.Status);
            termBuilder.HasIndex(x => x.ParentCode);
            termBuilder.HasIndex(x => x.Label);

            // Set-spanning uniqueness of codes within a taxonomy
            termBuilder.HasIndex("TaxonomyId", "Code").IsUnique();
        });
    }
}
