using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Infrastructure.BackgroundServices;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;
using Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.IdentityAccess.Infrastructure;

public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddScoped<PublishDomainEventsInterceptor>();

        services.AddDbContext<IdentityAccessDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<PublishDomainEventsInterceptor>();
            options.UseInMemoryDatabase("IdentityAccess")
                   .AddInterceptors(interceptor);
        });

        // Repositories
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();
        services.AddScoped<IRevokedTokenStore, RevokedTokenStore>();
        services.AddScoped<IAdminActionLogRepository, AdminActionLogRepository>();

        // Port adapters
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IBreachCheckPort, BreachCheckPortStub>();
        services.AddScoped<IJwtSigner, JwtSigner>();
        services.AddScoped<IOtpDeliveryPort, OtpDeliveryPortStub>();
        services.AddScoped<ITotpProvider, TotpProvider>();
        services.AddScoped<IRateLimiterPort, RateLimiterPortStub>();

        // Background services
        services.AddHostedService<OtpExpirySweepBackgroundService>();
        services.AddHostedService<SessionExpirySweepBackgroundService>();
        services.AddHostedService<CleanupBackgroundService>();

        return services;
    }
}
