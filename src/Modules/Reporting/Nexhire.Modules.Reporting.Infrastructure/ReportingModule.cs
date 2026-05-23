using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.Reporting.Core.Application.Projections;
using Nexhire.Modules.Reporting.Core.Domain.Ports;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Infrastructure.Adapters;
using Nexhire.Modules.Reporting.Infrastructure.Endpoints;
using Nexhire.Modules.Reporting.Infrastructure.Persistence;
using Nexhire.Modules.Reporting.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.Reporting.Infrastructure;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ReportingDbContext>(options => options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
        services.AddScoped<IReportRunRepository, ReportRunRepository>();
        services.AddScoped<IReportScheduleRepository, ReportScheduleRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Read stores
        services.AddScoped<IActivityReadStore, ActivityReadStore>();
        services.AddScoped<IAnalyticsReadStore, AnalyticsReadStore>();
        services.AddScoped<IPerformanceReadStore, PerformanceReadStore>();
        services.AddScoped<IReportAccessLogStore, ReportAccessLogStore>();
        services.AddScoped<IInboxStore, InboxStore>();

        // Projector service
        services.AddScoped<ProjectorService>();

        // Port adapters (stubs)
        services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        services.AddScoped<IReportRenderer, StubReportRenderer>();
        services.AddScoped<IColdStorageArchive, StubColdStorageArchive>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ReportingEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
