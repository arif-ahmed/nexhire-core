using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Ports;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Infrastructure.Adapters;
using Nexhire.Modules.SearchDiscovery.Infrastructure.Endpoints;
using Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence;
using Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure;

public static class SearchDiscoveryModule
{
    public static IServiceCollection AddSearchDiscoveryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<SearchDiscoveryDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IJobIndexEntryRepository, JobIndexEntryRepository>();
        services.AddScoped<IFavoriteJobRepository, FavoriteJobRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();
        services.AddScoped<ISearchSessionRepository, SearchSessionRepository>();
        services.AddScoped<IMatchScoreCacheRepository, MatchScoreCacheRepository>();
        services.AddScoped<IRecommendationCacheRepository, RecommendationCacheRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IRecommendationQueryApi, StubRecommendationQueryApi>();

        return services;
    }

    public static IEndpointRouteBuilder MapSearchDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        SearchDiscoveryEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
