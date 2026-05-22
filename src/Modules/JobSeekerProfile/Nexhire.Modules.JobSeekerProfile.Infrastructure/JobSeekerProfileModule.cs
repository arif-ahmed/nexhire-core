using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;
using Nexhire.Modules.JobSeekerProfile.Infrastructure.Endpoints;
using Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence;
using Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure;

public static class JobSeekerProfileModule
{
    public static IServiceCollection AddJobSeekerProfileModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<JobSeekerProfileDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register repositories and Unit of Work
        services.AddScoped<IJobSeekerProfileRepository, JobSeekerProfileRepository>();
        services.AddScoped<IProfileHistoryRepository, ProfileHistoryRepository>();
        services.AddScoped<IResumeRepository, ResumeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register external ports stub adapters
        services.AddScoped<IIdentityProvisioningApi, StubIdentityProvisioningApi>();
        services.AddScoped<IObjectStorage, StubObjectStorage>();
        services.AddScoped<IQrCodeGenerator, StubQrCodeGenerator>();
        services.AddScoped<IResumeParser, StubResumeParser>();
        services.AddScoped<ITaxonomyApi, StubTaxonomyApi>();
        services.AddScoped<IVirusScanner, StubVirusScanner>();

        return services;
    }

    public static IEndpointRouteBuilder MapJobSeekerProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        JobSeekerEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
