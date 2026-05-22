using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Services;
using Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;
using Nexhire.Modules.RecommendationEngine.Infrastructure.Endpoints;
using Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence;
using Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure;

public static class RecommendationEngineModule
{
    public static IServiceCollection AddRecommendationEngineModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<RecommendationEngineDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ISeekerMatchProfileRepository, SeekerMatchProfileRepository>();
        services.AddScoped<IPostingMatchProfileRepository, PostingMatchProfileRepository>();
        services.AddScoped<IEmbeddingRecordRepository, EmbeddingRecordRepository>();
        services.AddScoped<IMatchScoreRepository, MatchScoreRepository>();
        services.AddScoped<IJobRecommendationSetRepository, JobRecommendationSetRepository>();
        services.AddScoped<ICandidateShortlistRepository, CandidateShortlistRepository>();
        services.AddScoped<IMatchingWeightProfileRepository, MatchingWeightProfileRepository>();
        services.AddScoped<IMatchThresholdConfigurationRepository, MatchThresholdConfigurationRepository>();
        services.AddScoped<ITalentPoolRepository, TalentPoolRepository>();
        services.AddScoped<IRecommendationFeedbackRepository, RecommendationFeedbackRepository>();
        services.AddScoped<IRecommendationEngineUnitOfWork, UnitOfWork>();

        services.AddScoped<MatchScoringService>();
        services.AddScoped<RecommendationRankingService>();
        services.AddScoped<CandidateRankingService>();
        services.AddScoped<FitAnalysisService>();
        services.AddScoped<MatchThresholdResolver>();
        services.AddScoped<CandidatePrivacyFilter>();
        services.AddScoped<AbVariantAllocator>();
        services.AddScoped<ImpactPreviewCalculator>();

        services.AddScoped<IEmbeddingModelPort, StubEmbeddingModelPort>();
        services.AddScoped<IVectorIndexPort, StubVectorIndexPort>();
        services.AddScoped<INlpExtractionPort, StubNlpExtractionPort>();
        services.AddScoped<ICollaborativeFilteringPort, StubCollaborativeFilteringPort>();
        services.AddScoped<IEmployerAccessApi, StubEmployerAccessApi>();

        return services;
    }

    public static IEndpointRouteBuilder MapRecommendationEngineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RecommendationEngineEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
