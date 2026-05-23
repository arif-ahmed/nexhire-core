using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Helper methods for EF Core JSON column conversions.
/// Static methods are used because EF Core's HasConversion requires expression trees,
/// which cannot contain statement-body lambdas.
/// </summary>
file static class JsonConversions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string SerializeLocalizations(IReadOnlyDictionary<Language, LocalizedContent> v) =>
        JsonSerializer.Serialize(v.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), JsonOptions);

    public static Dictionary<Language, LocalizedContent> DeserializeLocalizations(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, LocalizedContent>>(json, JsonOptions)!
            .ToDictionary(kv => Enum.Parse<Language>(kv.Key), kv => kv.Value);

    public static string SerializeFaqLocalizations(IReadOnlyDictionary<Language, FaqContent> v) =>
        JsonSerializer.Serialize(v.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), JsonOptions);

    public static Dictionary<Language, FaqContent> DeserializeFaqLocalizations(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, FaqContent>>(json, JsonOptions)!
            .ToDictionary(kv => Enum.Parse<Language>(kv.Key), kv => kv.Value);

    public static string SerializeNames(IReadOnlyDictionary<Language, string> v) =>
        JsonSerializer.Serialize(v.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), JsonOptions);

    public static Dictionary<Language, string> DeserializeNames(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)!
            .ToDictionary(kv => Enum.Parse<Language>(kv.Key), kv => kv.Value);
}

internal sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AuthorUserId).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().IsRequired();
        builder.Property(a => a.PrimaryCategoryId);
        builder.Property(a => a.PublishedOnUtc);
        builder.Property(a => a.PreviousStatus).HasConversion<string>();
        builder.Property(a => a.CreatedOnUtc).IsRequired();
        builder.Property(a => a.UpdatedOnUtc).IsRequired();

        // Schedule VO as owned
        builder.OwnsOne(a => a.PublicationSchedule, schedule =>
        {
            schedule.Property(s => s.PublishAtUtc).HasColumnName("schedule_publish_at");
        });

        // Media as JSON
        builder.Property(a => a.Media)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<MediaReference>>(v, JsonOptions) ?? new());

        // Localizations as JSON
        builder.Property(a => a.Localizations)
            .HasConversion(
                v => JsonConversions.SerializeLocalizations(v),
                v => JsonConversions.DeserializeLocalizations(v));

        // Tags as JSON
        builder.Property(a => a.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ArticleTag>>(v, JsonOptions) ?? new());

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.PrimaryCategoryId);
        builder.HasIndex(a => new { a.Status, a.PublishedOnUtc });
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CreatedOnUtc).IsRequired();
        builder.Property(c => c.UpdatedOnUtc).IsRequired();

        // Names as JSON column (IReadOnlyDictionary<Language, string>)
        builder.Property(c => c.Names)

            .HasConversion(
                v => JsonConversions.SerializeNames(v),
                v => JsonConversions.DeserializeNames(v));
    }
}

internal sealed class FaqEntryConfiguration : IEntityTypeConfiguration<FaqEntry>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Configure(EntityTypeBuilder<FaqEntry> builder)
    {
        builder.ToTable("faq_entries");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Kind).HasConversion<string>().IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().IsRequired();
        builder.Property(f => f.CreatedOnUtc).IsRequired();
        builder.Property(f => f.UpdatedOnUtc).IsRequired();

        // VisibleRoles as JSON column
        builder.Property(f => f.VisibleRoles)

            .HasConversion(
                v => JsonSerializer.Serialize(v!.Roles.Select(r => r.ToString()).ToList(), JsonOptions),
                v => VisibleRoleSet.Create(
                    JsonSerializer.Deserialize<List<string>>(v, JsonOptions)
                        .Select(s => Enum.Parse<VisibleRole>(s))).Value!);

        // ContextKeys as JSON column
        builder.Property(f => f.ContextKeys)

            .HasConversion(
                v => JsonSerializer.Serialize(v.ToList(), JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions).AsReadOnly());

        // MultimediaBlocks as JSON column
        builder.Property(f => f.MultimediaBlocks)

            .HasConversion(
                v => JsonSerializer.Serialize(v.ToList(), JsonOptions),
                v => JsonSerializer.Deserialize<List<MultimediaBlock>>(v, JsonOptions).AsReadOnly());

        // Localizations as JSON column (IReadOnlyDictionary<Language, FaqContent>)
        builder.Property(f => f.Localizations)

            .HasConversion(
                v => JsonConversions.SerializeFaqLocalizations(v),
                v => JsonConversions.DeserializeFaqLocalizations(v));

        // TopicIds via JSON (simple GUID list)
        builder.Property(f => f.TopicIds)

            .HasConversion(
                v => JsonSerializer.Serialize(v.ToList(), JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions).AsReadOnly());

        builder.HasIndex(f => f.Status);
    }
}

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.CreatedOnUtc).IsRequired();
        builder.Property(t => t.UpdatedOnUtc).IsRequired();

        // Names as JSON column (IReadOnlyDictionary<Language, string>)
        builder.Property(t => t.Names)

            .HasConversion(
                v => JsonConversions.SerializeNames(v),
                v => JsonConversions.DeserializeNames(v));
    }
}

internal sealed class GuidedTourConfiguration : IEntityTypeConfiguration<GuidedTour>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Configure(EntityTypeBuilder<GuidedTour> builder)
    {
        builder.ToTable("guided_tours");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Language).HasConversion<string>().IsRequired();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(1000);
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.CreatedOnUtc).IsRequired();
        builder.Property(t => t.UpdatedOnUtc).IsRequired();

        // TargetAudience as JSON column
        builder.Property(t => t.TargetAudience)

            .HasConversion(
                v => JsonSerializer.Serialize(v.Audiences.Select(a => a.ToString()).ToList(), JsonOptions),
                v => AudienceSet.Create(
                    JsonSerializer.Deserialize<List<string>>(v, JsonOptions)
                        .Select(s => Enum.Parse<Audience>(s))).Value!);

        // Tour steps as owned collection (TourStep is Entity<Guid> with its own Id)
        builder.OwnsMany(t => t.Steps, stepBuilder =>
        {
            stepBuilder.ToTable("tour_steps");

            stepBuilder.HasKey(s => s.Id);

            stepBuilder.WithOwner().HasForeignKey("GuidedTourId");

            stepBuilder.Property(s => s.Order).IsRequired();
            stepBuilder.Property(s => s.TargetSelector).IsRequired().HasMaxLength(500).HasColumnName("target_selector");
            stepBuilder.Property(s => s.TooltipText).IsRequired().HasMaxLength(500).HasColumnName("tooltip_text");

            stepBuilder.OwnsOne(s => s.Action, actionBuilder =>
            {
                actionBuilder.Property(a => a.Kind).HasConversion<string>().HasColumnName("action_kind");
                actionBuilder.Property(a => a.Payload).HasColumnName("action_payload");
            });

            // Shadow FK index for uniqueness of step order within a tour
            stepBuilder.HasIndex("GuidedTourId", nameof(TourStep.Order)).IsUnique();
        });
    }
}

internal sealed class ContentPreferenceConfiguration : IEntityTypeConfiguration<ContentPreference>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Configure(EntityTypeBuilder<ContentPreference> builder)
    {
        builder.ToTable("content_preferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.PreferredLanguage).HasConversion<string>().IsRequired();
        builder.Property(p => p.UpdatedOnUtc).IsRequired();

        builder.Property(p => p.IncludedCategoryIds)

            .HasConversion(
                v => JsonSerializer.Serialize(v.ToList(), JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions).AsReadOnly());

        builder.Property(p => p.HiddenCategoryIds)

            .HasConversion(
                v => JsonSerializer.Serialize(v.ToList(), JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions).AsReadOnly());
    }
}

internal sealed class HelpFeedbackConfiguration : IEntityTypeConfiguration<HelpFeedback>
{
    public void Configure(EntityTypeBuilder<HelpFeedback> builder)
    {
        builder.ToTable("help_feedback");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FaqEntryId).IsRequired();
        builder.Property(f => f.WasHelpful).IsRequired();
        builder.Property(f => f.Reason).HasConversion<string>();
        builder.Property(f => f.Comment).HasMaxLength(2000);
        builder.Property(f => f.SubmittedByRole).HasMaxLength(50);
        builder.Property(f => f.Language).HasConversion<string>().IsRequired();
        builder.Property(f => f.SubmittedOnUtc).IsRequired();
        builder.HasIndex(f => f.FaqEntryId);
    }
}
