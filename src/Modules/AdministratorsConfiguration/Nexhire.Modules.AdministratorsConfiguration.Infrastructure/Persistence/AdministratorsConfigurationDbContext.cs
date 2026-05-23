using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence;

public sealed class AdministratorsConfigurationDbContext : DbContext, IOutboxInboxDbContext, IAdministratorsConfigurationUnitOfWork
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public AdministratorsConfigurationDbContext(
        DbContextOptions<AdministratorsConfigurationDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<Taxonomy> Taxonomies => Set<Taxonomy>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin_config");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministratorsConfigurationDbContext).Assembly);
        ConfigureOutboxInbox(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    private static void ConfigureOutboxInbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Content).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.OccurredOnUtc).IsRequired();
            builder.Property(x => x.ProcessedOnUtc);
            builder.Property(x => x.Error).HasMaxLength(4000);
            builder.HasIndex(x => x.ProcessedOnUtc);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
            builder.Property(x => x.ReceivedOnUtc).IsRequired();
            builder.Property(x => x.ProcessedOnUtc);
            builder.HasIndex(x => x.ProcessedOnUtc);
        });
    }
}
