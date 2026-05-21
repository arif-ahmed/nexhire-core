using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Adapters;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Endpoints;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure;

public static class EmployerProfilesModule
{
    public static IServiceCollection AddEmployerProfilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<EmployerProfilesDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEmployerProfileRepository, EmployerProfileRepository>();
        services.AddScoped<IShortlistRepository, ShortlistRepository>();
        services.AddScoped<IDashboardProjectionStore, DashboardProjectionStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IIdentityProvisioningApi, StubIdentityProvisioningApi>();
        services.AddScoped<IVirusScanner, StubVirusScanner>();
        services.AddScoped<IObjectStorage, StubObjectStorage>();

        return services;
    }

    public static IEndpointRouteBuilder MapEmployerProfilesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        EmployerEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
