using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.Notification.Domain.Ports;
using Nexhire.Modules.Notification.Domain.Repositories;
using Nexhire.Modules.Notification.Domain.Services;
using Nexhire.Modules.Notification.Application.PublicApi;
using Nexhire.Modules.Notification.Infrastructure.Adapters;
using Nexhire.Modules.Notification.Infrastructure.Background;
using Nexhire.Modules.Notification.Infrastructure.Endpoints;
using Nexhire.Modules.Notification.Infrastructure.Persistence;
using Nexhire.Modules.Notification.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.Notification.Infrastructure;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<NotificationDbContext>(options => 
            options.UseNpgsql(connectionString));

        // Register Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRecipientPreferencesRepository, RecipientPreferencesRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IDigestRepository, DigestRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationUnitOfWork, NotificationUnitOfWork>();

        // Register Domain Services
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();
        services.AddScoped<IChannelFanoutPlanner, ChannelFanoutPlanner>();
        services.AddScoped<IFrequencyCapEvaluator, FrequencyCapEvaluator>();
        services.AddScoped<IDndScheduleCalculator, DndScheduleCalculator>();
        services.AddScoped<IDigestAssembler, DigestAssembler>();

        // Register Port Adapters (Including high-fidelity stubs for testing)
        services.AddScoped<IEmailChannel, EmailChannelStub>();
        services.AddScoped<ISmsChannel, SmsChannelStub>();
        services.AddScoped<IRealtimePush, RealtimePushStub>();
        services.AddScoped<IDncRegistry, DncRegistryStub>();
        services.AddScoped<INotificationPublicApi, Adapters.NotificationPublicApi>();

        // Register Background Scheduled Sweep Workers
        services.AddHostedService<OutboxRelayWorker>();
        services.AddHostedService<DigestSchedulerWorker>();
        services.AddHostedService<DndReleaseWorker>();
        services.AddHostedService<SoftBounceRetryWorker>();
        services.AddHostedService<RetentionWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        NotificationEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
