using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Infrastructure.Adapters;
using Nexhire.Modules.JobApplication.Infrastructure.Background;
using Nexhire.Modules.JobApplication.Infrastructure.Endpoints;
using Nexhire.Modules.JobApplication.Infrastructure.Persistence;
using Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.JobApplication.Infrastructure;

public static class JobApplicationModule
{
    public static IServiceCollection AddJobApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<JobApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IIdempotencyKeyStore, IdempotencyKeyStore>();
        services.AddScoped<IJobApplicationUnitOfWork, JobApplicationUnitOfWork>();

        // Register stub adapters
        services.AddScoped<IJobPostingApi, StubJobPostingApi>();
        services.AddScoped<IJobSeekerProfileApi, StubJobSeekerProfileApi>();
        services.AddScoped<IMatchRankingPublicApi, StubMatchRankingPublicApi>();
        services.AddScoped<IEmployerAccessApi, StubEmployerAccessApi>();

        // Register background services
        services.AddHostedService<IdempotencyKeyPurgeBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapJobApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        JobApplicationEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
