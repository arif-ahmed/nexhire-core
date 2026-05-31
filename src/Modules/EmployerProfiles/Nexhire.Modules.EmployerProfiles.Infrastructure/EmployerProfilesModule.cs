using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.EmployerProfiles.Contracts;
using Nexhire.Modules.EmployerProfiles.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Adapters;
using Nexhire.Modules.EmployerProfiles.Infrastructure.BackgroundServices;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;
using Nexhire.Modules.EmployerProfiles.Infrastructure.PublicApi;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure;

public static class EmployerProfilesModule
{
    public static IServiceCollection AddEmployerProfilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireUsersManage", policy =>
                policy.RequireClaim("permission", "users:manage"));
        });

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

        services.AddScoped<IEmployerProfilePublicApi, EmployerProfilePublicApiAdapter>();

        services.AddHostedService<EmployerProfilesOutboxRelayBackgroundService>();

        return services;
    }
}
