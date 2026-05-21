using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Shared.Infrastructure.Behaviors;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.OpenApi;
using System.Reflection;

namespace Nexhire.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] moduleAssemblies)
    {
        // 1. Register MediatR and configure our pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(moduleAssemblies);
            
            // Register behaviors in flow-through order (Logging wraps Validation)
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 2. Register FluentValidation schemas from all modules
        services.AddValidatorsFromAssemblies(moduleAssemblies, ServiceLifetime.Transient);

        // 3. Register our EF Core Domain Event Interceptor
        services.AddScoped<PublishDomainEventsInterceptor>();

        // 4. Register OpenAPI Document Generation
        services.AddOpenApiDocumentation();

        return services;
    }
}
