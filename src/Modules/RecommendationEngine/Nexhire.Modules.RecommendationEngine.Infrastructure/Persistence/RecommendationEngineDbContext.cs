using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence;

public class RecommendationEngineDbContext : DbContext, IOutboxInboxDbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public RecommendationEngineDbContext(
        DbContextOptions<RecommendationEngineDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<SeekerMatchProfile> SeekerMatchProfiles => Set<SeekerMatchProfile>();
    public DbSet<PostingMatchProfile> PostingMatchProfiles => Set<PostingMatchProfile>();
    public DbSet<EmbeddingRecord> EmbeddingRecords => Set<EmbeddingRecord>();
    public DbSet<MatchScore> MatchScores => Set<MatchScore>();
    public DbSet<JobRecommendationSet> JobRecommendationSets => Set<JobRecommendationSet>();
    public DbSet<CandidateShortlist> CandidateShortlists => Set<CandidateShortlist>();
    public DbSet<MatchingWeightProfile> MatchingWeightProfiles => Set<MatchingWeightProfile>();
    public DbSet<MatchThresholdConfiguration> MatchThresholdConfigurations => Set<MatchThresholdConfiguration>();
    public DbSet<TalentPool> TalentPools => Set<TalentPool>();
    public DbSet<RecommendationFeedback> RecommendationFeedback => Set<RecommendationFeedback>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("recommendation_engine");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecommendationEngineDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
