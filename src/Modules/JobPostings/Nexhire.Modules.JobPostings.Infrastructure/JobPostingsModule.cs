using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.Services;
using Nexhire.Modules.JobPostings.Core.JobPostings.Commands;
using Nexhire.Modules.JobPostings.Infrastructure.Adapters;
using Nexhire.Modules.JobPostings.Infrastructure.Background;
using Nexhire.Modules.JobPostings.Infrastructure.Endpoints;
using Nexhire.Modules.JobPostings.Infrastructure.Persistence;
using Nexhire.Modules.JobPostings.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.JobPostings.Infrastructure;

public static class JobPostingsModule
{
    public static IServiceCollection AddJobPostingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<JobPostingsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IJobPostingRepository, JobPostingRepository>();
        services.AddScoped<IPostingAuditTrailRepository, PostingAuditTrailRepository>();
        services.AddScoped<IEmployerStandingStore, EmployerStandingStore>();
        services.AddScoped<IPostingMetricsStore, PostingMetricsStore>();
        services.AddScoped<IJobPostingsUnitOfWork, JobPostingsUnitOfWork>();
        services.AddScoped<ITaxonomyApi, StubTaxonomyApi>();
        services.AddScoped<IAuditTrailExporter, CsvAuditTrailExporter>();
        services.AddSingleton<SchemaOrgStandardizer>();
        services.AddSingleton<PostingExpirationPolicy>();
        services.AddSingleton<JobPostingRenewalService>();
        services.AddScoped<RenewJobPostingCommandHandler>();
        services.AddHostedService<PostingExpirationBackgroundService>();
        services.AddHostedService<JobPostingsOutboxRelayBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapJobPostingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        JobPostingEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
