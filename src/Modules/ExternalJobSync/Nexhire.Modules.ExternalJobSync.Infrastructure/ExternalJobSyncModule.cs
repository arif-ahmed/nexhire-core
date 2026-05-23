using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Services;
using Nexhire.Modules.ExternalJobSync.Infrastructure.Adapters;
using Nexhire.Modules.ExternalJobSync.Infrastructure.Background;
using Nexhire.Modules.ExternalJobSync.Infrastructure.Endpoints;
using Nexhire.Modules.ExternalJobSync.Infrastructure.Persistence;
using Nexhire.Modules.ExternalJobSync.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure;

public static class ExternalJobSyncModule
{
    public static IServiceCollection AddExternalJobSyncModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ExternalJobSyncDbContext>(options => options.UseNpgsql(connectionString));

        // Register Repositories
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IExternalConnectorRepository, ExternalConnectorRepository>();
        services.AddScoped<IMappingProfileRepository, MappingProfileRepository>();
        services.AddScoped<ISyncRecordRepository, SyncRecordRepository>();
        services.AddScoped<IVerificationRequestRepository, VerificationRequestRepository>();
        services.AddScoped<IGovernmentAuditRepository, GovernmentAuditRepository>();
        services.AddScoped<IApiVersionRegistryRepository, ApiVersionRegistryRepository>();
        services.AddScoped<IExternalJobSyncUnitOfWork, ExternalJobSyncUnitOfWork>();

        // Register Port Adapters (Including high-fidelity stubs for external systems)
        services.AddScoped<ICredentialEncryptionPort, CredentialEncryptionAdapter>();
        services.AddScoped<IJobPostingPublicApi, JobPostingPublicApiStub>();
        services.AddScoped<ITaxonomyApi, TaxonomyApiStub>();
        services.AddScoped<ITokenValidationApi, TokenValidationApiStub>();
        services.AddScoped<IExternalPortalPort, ExternalPortalPortStub>();
        services.AddScoped<IGovernmentRegistryPort, GovernmentRegistryPortStub>();
        services.AddScoped<IGeocodingPort, GeocodingPortStub>();

        // Register Background Scheduled Sweep Workers
        services.AddHostedService<SyncSchedulerWorker>();
        services.AddHostedService<StaleRecordArchiverWorker>();
        services.AddHostedService<ApiKeyExpirySweepWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapExternalJobSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ExternalJobSyncEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
