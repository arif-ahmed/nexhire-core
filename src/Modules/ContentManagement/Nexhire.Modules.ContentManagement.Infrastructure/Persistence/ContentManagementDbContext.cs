using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Persistence;

public sealed class ContentManagementDbContext : DbContext, IOutboxInboxDbContext, IContentManagementUnitOfWork
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public ContentManagementDbContext(
        DbContextOptions<ContentManagementDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FaqEntry> FaqEntries => Set<FaqEntry>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<GuidedTour> GuidedTours => Set<GuidedTour>();
    public DbSet<ContentPreference> ContentPreferences => Set<ContentPreference>();
    public DbSet<HelpFeedback> HelpFeedbacks => Set<HelpFeedback>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_management");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentManagementDbContext).Assembly);
        ConfigureOutboxInbox(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    public new async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await base.SaveChangesAsync(ct);
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
