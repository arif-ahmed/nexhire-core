using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Persistence.Configurations;

public sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("partners");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactEmail)
            .HasConversion(v => v.Value, v => EmailAddress.Create(v).Value)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(x => x.Website).HasMaxLength(200);
        builder.Property(x => x.CompanyInfo).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.PublicAttribution).IsRequired();
        builder.Property(x => x.RegisteredOnUtc).IsRequired();
        builder.Property(x => x.ActivatedOnUtc);

        builder.Property(x => x.RateLimit)
            .HasConversion(JsonConversion.NullableConverter<RateLimit>())
            .HasColumnType("jsonb");

        builder.Property<List<string>>("_ipWhitelist")
            .HasColumnName("ip_whitelist")
            .HasConversion(JsonConversion.Converter<List<string>>())
            .HasColumnType("jsonb");

        builder.HasMany(x => x.ApiKeys)
            .WithOne()
            .HasForeignKey("partner_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Partner.ApiKeys))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ExpiresOnUtc);
        builder.Property(x => x.IssuedOnUtc).IsRequired();
        builder.Property(x => x.RevokedOnUtc);

        builder.HasIndex(x => x.KeyHash).IsUnique();
    }
}

public sealed class ExternalConnectorConfiguration : IEntityTypeConfiguration<ExternalConnector>
{
    public void Configure(EntityTypeBuilder<ExternalConnector> builder)
    {
        builder.ToTable("external_connectors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PortalName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ApiEndpoint).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ConnectionStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LastPullOnUtc);
        builder.Property(x => x.LastPushOnUtc);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();

        builder.Property(x => x.Credentials)
            .HasConversion(JsonConversion.Converter<EncryptedCredentials>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.SyncOptions)
            .HasConversion(JsonConversion.Converter<SyncOptions>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasMany(x => x.WebhookSubscriptions)
            .WithOne()
            .HasForeignKey("connector_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ExternalConnector.WebhookSubscriptions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CallbackPath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SigningAlgorithm).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();

        builder.Property(x => x.SigningSecret)
            .HasConversion(JsonConversion.Converter<EncryptedCredentials>())
            .HasColumnType("jsonb")
            .IsRequired();
    }
}

public sealed class MappingProfileConfiguration : IEntityTypeConfiguration<MappingProfile>
{
    public void Configure(EntityTypeBuilder<MappingProfile> builder)
    {
        builder.ToTable("mapping_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PortalName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();

        builder.HasMany(x => x.FieldMappings)
            .WithOne()
            .HasForeignKey("profile_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(MappingProfile.FieldMappings))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FieldMappingConfiguration : IEntityTypeConfiguration<FieldMapping>
{
    public void Configure(EntityTypeBuilder<FieldMapping> builder)
    {
        builder.ToTable("field_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourcePath).HasMaxLength(250).IsRequired();
        builder.Property(x => x.TargetPath).HasMaxLength(250).IsRequired();
        builder.Property(x => x.TransformKind).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.TransformArgs).HasMaxLength(1000);
        builder.Property(x => x.IsRequired).IsRequired();
    }
}

public sealed class SyncRecordConfiguration : IEntityTypeConfiguration<SyncRecord>
{
    public void Configure(EntityTypeBuilder<SyncRecord> builder)
    {
        builder.ToTable("sync_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.PartnerId);
        builder.Property(x => x.ConnectorId);
        builder.Property(x => x.InternalJobId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.RawPayload).HasColumnType("text").IsRequired();
        builder.Property(x => x.NormalisedSnapshot).HasColumnType("text");
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.LastSyncOnUtc).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();

        // Flatten ExternalRef VO to columns
        builder.OwnsOne(x => x.ExternalRef, nav =>
        {
            nav.Property(p => p.PortalName).HasColumnName("portal_name").HasMaxLength(150).IsRequired();
            nav.Property(p => p.ExternalJobId).HasColumnName("external_job_id").HasMaxLength(200).IsRequired();
            
            nav.HasIndex("PortalName", "ExternalJobId").IsUnique();
        });

        builder.HasMany(x => x.Attempts)
            .WithOne()
            .HasForeignKey("sync_record_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(SyncRecord.Attempts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SyncAttemptConfiguration : IEntityTypeConfiguration<SyncAttempt>
{
    public void Configure(EntityTypeBuilder<SyncAttempt> builder)
    {
        builder.ToTable("sync_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AttemptNo).IsRequired();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ResponseCode);
        builder.Property(x => x.Detail).HasColumnType("text");
        builder.Property(x => x.ProcessingMs).IsRequired();
        builder.Property(x => x.AttemptedOnUtc).IsRequired();
        builder.Property(x => x.WasManualOverride).IsRequired();
    }
}

public sealed class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequest>
{
    public void Configure(EntityTypeBuilder<VerificationRequest> builder)
    {
        builder.ToTable("verification_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.SubjectUserId);
        builder.Property(x => x.SubjectJobSeekerProfileId);
        builder.Property(x => x.SubjectEmployerId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.CachedUntilUtc);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();

        builder.Property(x => x.Registry)
            .HasConversion(JsonConversion.Converter<Registry>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Consent)
            .HasConversion(JsonConversion.Converter<ConsentRecord>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.RequestPayload)
            .HasConversion(JsonConversion.NullableConverter<MinimisedRequestPayload>())
            .HasColumnType("jsonb");

        builder.Property(x => x.Result)
            .HasConversion(JsonConversion.NullableConverter<VerificationResult>())
            .HasColumnType("jsonb");
    }
}

public sealed class GovernmentAuditEntryConfiguration : IEntityTypeConfiguration<GovernmentAuditEntry>
{
    public void Configure(EntityTypeBuilder<GovernmentAuditEntry> builder)
    {
        builder.ToTable("government_audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VerificationRequestId);
        builder.Property(x => x.ActorId);
        builder.Property(x => x.RegistryName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.QueryParameters).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ResultCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ResponseSizeBytes);
        builder.Property(x => x.TransformationsApplied).HasMaxLength(500);
        builder.Property(x => x.ConsentStatusAtTime).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IntegrityHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OccurredOnUtc).IsRequired();

        builder.HasIndex(x => x.VerificationRequestId);
        builder.HasIndex(x => x.OccurredOnUtc);
    }
}

public sealed class ApiVersionRegistryConfiguration : IEntityTypeConfiguration<ApiVersionRegistry>
{
    public void Configure(EntityTypeBuilder<ApiVersionRegistry> builder)
    {
        builder.ToTable("api_version_registries");
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey("registry_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ApiVersionRegistry.Versions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ApiVersionConfiguration : IEntityTypeConfiguration<ApiVersion>
{
    public void Configure(EntityTypeBuilder<ApiVersion> builder)
    {
        builder.ToTable("api_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReleasedOnUtc).IsRequired();
        builder.Property(x => x.DeprecationAnnouncedOnUtc);
        builder.Property(x => x.SunsetOnUtc);
        builder.Property(x => x.MigrationGuideUrl).HasMaxLength(500);

        builder.HasIndex(x => x.Version).IsUnique();
    }
}
