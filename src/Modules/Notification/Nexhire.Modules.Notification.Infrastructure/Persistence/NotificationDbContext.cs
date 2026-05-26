using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Shared.Infrastructure.Messaging;
using Nexhire.Modules.Notification.Domain.Aggregates;
using Nexhire.Modules.Notification.Domain;
using NotificationAggregate = Nexhire.Modules.Notification.Domain.Aggregates.Notification;

namespace Nexhire.Modules.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext, IOutboxInboxDbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

    public DbSet<NotificationAggregate> Notifications { get; set; } = null!;
    public DbSet<DeliveryAttempt> DeliveryAttempts { get; set; } = null!;
    public DbSet<RecipientPreferences> RecipientPreferences { get; set; } = null!;
    public DbSet<ChannelTypePreference> ChannelTypePreferences { get; set; } = null!;
    public DbSet<ConsentRecord> ConsentRecords { get; set; } = null!;
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = null!;
    public DbSet<TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<Digest> Digests { get; set; } = null!;
    public DbSet<DigestItem> DigestItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");

        // Outbox & Inbox Mappings
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(o => o.Id);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(i => i.Id);
        });

        // 1. Notification Template Mapping
        modelBuilder.Entity<NotificationTemplate>(builder =>
        {
            builder.ToTable("notification_templates");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
                .HasConversion(id => id.Value, value => new NotificationTemplateId(value));

            builder.Property(t => t.Channel).HasConversion<string>();
            builder.Property(t => t.Type).HasConversion<string>();
            builder.HasIndex(t => new { t.Channel, t.Type }).IsUnique();

            builder.Property(t => t.CurrentVersion)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    s => JsonSerializer.Deserialize<TemplateVersion>(s, JsonOptions)!);
        });

        modelBuilder.Entity<TemplateVersion>(builder =>
        {
            builder.ToTable("template_versions");
            builder.HasKey(v => v.CreatedByUserId); // Shadow PK or simple PK mappings
            builder.Property(v => v.Placeholders)
                .HasColumnType("jsonb")
                .HasConversion(
                    p => JsonSerializer.Serialize(p, JsonOptions),
                    s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions)!);
        });

        // 2. RecipientPreferences Mapping
        modelBuilder.Entity<RecipientPreferences>(builder =>
        {
            builder.ToTable("recipient_preferences");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .HasConversion(id => id.Value, value => new RecipientPreferencesId(value));

            builder.HasIndex(r => r.UserId).IsUnique();

            builder.Property(r => r.EmailContact)
                .HasColumnType("jsonb")
                .HasConversion(
                    e => JsonSerializer.Serialize(e, JsonOptions),
                    s => JsonSerializer.Deserialize<EmailContactPoint>(s, JsonOptions)!);

            builder.Property(r => r.SmsContact)
                .HasColumnType("jsonb")
                .HasConversion(
                    p => p == null ? null : JsonSerializer.Serialize(p, JsonOptions),
                    s => string.IsNullOrEmpty(s) ? null : JsonSerializer.Deserialize<PhoneContactPoint>(s, JsonOptions));

            builder.Property(r => r.DoNotDisturb)
                .HasColumnType("jsonb")
                .HasConversion(
                    d => d == null ? null : JsonSerializer.Serialize(d, JsonOptions),
                    s => string.IsNullOrEmpty(s) ? null : JsonSerializer.Deserialize<DndWindow>(s, JsonOptions));

            builder.Property(r => r.VersionToken).IsConcurrencyToken();

            builder.HasMany(r => r.ChannelTypePrefs)
                .WithOne()
                .HasForeignKey("RecipientPreferencesId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Consents)
                .WithOne()
                .HasForeignKey("RecipientPreferencesId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChannelTypePreference>(builder =>
        {
            builder.ToTable("channel_type_preferences");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Channel).HasConversion<string>();
            builder.Property(c => c.Type).HasConversion<string>();
            builder.Property(c => c.Frequency).HasConversion<string>();
            builder.Property(c => c.ToastMode).HasConversion<string>();
        });

        modelBuilder.Entity<ConsentRecord>(builder =>
        {
            builder.ToTable("consent_records");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Channel).HasConversion<string>();
        });

        // 3. Digest Mapping
        modelBuilder.Entity<Digest>(builder =>
        {
            builder.ToTable("digests");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id)
                .HasConversion(id => id.Value, value => new DigestId(value));

            builder.Property(d => d.Channel).HasConversion<string>();
            builder.Property(d => d.Window).HasConversion<string>();
            builder.Property(d => d.Status).HasConversion<string>();

            builder.HasMany(d => d.Items)
                .WithOne()
                .HasForeignKey("DigestId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DigestItem>(builder =>
        {
            builder.ToTable("digest_items");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.NotificationId)
                .HasConversion(id => id.Value, value => new NotificationId(value));
            builder.Property(d => d.Type).HasConversion<string>();
        });

        // 4. Notification Mapping
        modelBuilder.Entity<NotificationAggregate>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id)
                .HasConversion(id => id.Value, value => new NotificationId(value));

            builder.Property(n => n.Channel).HasConversion<string>();
            builder.Property(n => n.Type).HasConversion<string>();
            builder.Property(n => n.Priority).HasConversion<string>();
            builder.Property(n => n.DeliveryStatus).HasConversion<string>();

            builder.Property(n => n.SourceEvent)
                .HasColumnType("jsonb")
                .HasConversion(
                    se => JsonSerializer.Serialize(se, JsonOptions),
                    s => JsonSerializer.Deserialize<SourceEventRef>(s, JsonOptions)!);

            builder.Property(n => n.Payload)
                .HasColumnType("jsonb")
                .HasConversion(
                    p => JsonSerializer.Serialize(p, JsonOptions),
                    s => JsonSerializer.Deserialize<NotificationPayload>(s, JsonOptions)!);

            builder.Property(n => n.Rendered)
                .HasColumnType("jsonb")
                .HasConversion(
                    r => r == null ? null : JsonSerializer.Serialize(r, JsonOptions),
                    s => string.IsNullOrEmpty(s) ? null : JsonSerializer.Deserialize<RenderedMessage>(s, JsonOptions));

            builder.Property(n => n.Engagement)
                .HasColumnType("jsonb")
                .HasConversion(
                    e => JsonSerializer.Serialize(e, JsonOptions),
                    s => JsonSerializer.Deserialize<EngagementState>(s, JsonOptions)!);

            builder.Property(n => n.VersionToken).IsConcurrencyToken();

            builder.HasMany(n => n.Attempts)
                .WithOne()
                .HasForeignKey("NotificationId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryAttempt>(builder =>
        {
            builder.ToTable("delivery_attempts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Channel).HasConversion<string>();
            builder.Property(a => a.Outcome).HasConversion<string>();
        });
    }
}
