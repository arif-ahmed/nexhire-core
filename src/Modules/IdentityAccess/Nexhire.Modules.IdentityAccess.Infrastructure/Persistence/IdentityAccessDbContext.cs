using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;

public class IdentityAccessDbContext : DbContext, IOutboxInboxDbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public IdentityAccessDbContext(
        DbContextOptions<IdentityAccessDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<UserAccount>     UserAccounts    => Set<UserAccount>();
    public DbSet<OtpChallenge>    OtpChallenges   => Set<OtpChallenge>();
    public DbSet<AdminActionLog>  AdminActionLogs => Set<AdminActionLog>();
    public DbSet<RevokedToken>    RevokedTokens   => Set<RevokedToken>();

    // IOutboxInboxDbContext — integration events written transactionally via PublishDomainEventsInterceptor
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage>  InboxMessages  => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity_access");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityAccessDbContext).Assembly);

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
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
            b.Property(x => x.Content).HasColumnName("content").IsRequired();
            b.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
            b.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
            b.Property(x => x.Error).HasColumnName("error");
            b.HasIndex(x => x.ProcessedOnUtc);
        });

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
            b.Property(x => x.ReceivedOnUtc).HasColumnName("received_on_utc").IsRequired();
            b.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
        });
    }
}
