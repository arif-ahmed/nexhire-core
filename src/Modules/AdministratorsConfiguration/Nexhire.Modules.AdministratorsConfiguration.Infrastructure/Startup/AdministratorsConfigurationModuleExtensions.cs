using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Ports;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Adapters;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Background;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Endpoints;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.IntegrationEvents.Publishers;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Services;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure;

public static class AdministratorsConfigurationModuleExtensions
{
    public static IServiceCollection AddAdministratorsConfigurationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        // 1. Register DbContext and migrations config
        services.AddDbContext<AdministratorsConfigurationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Register Repository & UnitOfWork
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<IAdministratorsConfigurationUnitOfWork>(provider =>
            provider.GetRequiredService<AdministratorsConfigurationDbContext>());

        // 3. Register Domain Services
        services.AddScoped<TaxonomyImportService>();

        // 4. Register CsvReader Adapter
        services.AddScoped<ICsvReader, CsvReader>();

        // 5. Register in-memory cache and Cached TaxonomyApi service
        services.AddMemoryCache();
        services.AddScoped<TaxonomyApiImpl>();
        services.AddScoped<ITaxonomyApi>(provider => provider.GetRequiredService<TaxonomyApiImpl>());
        
        // Wire up MediatR invalidation handler to clear cache on taxonomy version bump
        services.AddScoped<INotificationHandler<TaxonomyUpdatedDomainEvent>>(provider => 
            provider.GetRequiredService<TaxonomyApiImpl>());

        // 6. Register Domain-to-Integration outbox mapping publishers
        services.AddScoped<INotificationHandler<TaxonomyTermAddedDomainEvent>, IntegrationEventPublisher>();
        services.AddScoped<INotificationHandler<TaxonomyTermDeprecatedDomainEvent>, IntegrationEventPublisher>();
        services.AddScoped<INotificationHandler<TaxonomyUpdatedDomainEvent>, IntegrationEventPublisher>();

        // 7. Register outbox background worker
        services.AddHostedService<AdministratorsConfigurationOutboxRelayBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapAdministratorsConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        TaxonomyEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
