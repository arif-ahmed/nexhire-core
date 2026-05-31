using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;
using Nexhire.Modules.EmployerProfiles.Infrastructure;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Infrastructure;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Infrastructure;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Infrastructure;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Infrastructure;
using Nexhire.Modules.RecommendationEngine.Infrastructure;
using Nexhire.Modules.IdentityAccess.Infrastructure;
using Nexhire.Modules.IdentityAccess.Presentation;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Infrastructure;
using Nexhire.Modules.Reporting.Infrastructure;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Infrastructure;
using Nexhire.Modules.ContentManagement.Infrastructure.Startup;
using Nexhire.Shared.Infrastructure;
using Nexhire.Shared.Infrastructure.OpenApi;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.Notification.Infrastructure;
using Nexhire.Api;
using Nexhire.Api.Adapters.IdentityAccess;
using Nexhire.Api.Middleware;
using Nexhire.Modules.IdentityAccess.Contracts;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Define active module assemblies for dynamic scanning (MediatR CQRS and FluentValidation schemas)
var moduleAssemblies = new[]
{
    typeof(Nexhire.Modules.IdentityAccess.Domain.Domain.UserAccount).Assembly,
    typeof(Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential.ProvisionCredentialCommand).Assembly,
    typeof(IdentityAccessModule).Assembly,
    typeof(Nexhire.Modules.IdentityAccess.Contracts.Events.UserRegisteredIntegrationEvent).Assembly,
    typeof(EmployerProfile).Assembly,
    typeof(EmployerProfilesModule).Assembly,
    typeof(JobPosting).Assembly,
    typeof(JobPostingsModule).Assembly,
    typeof(JobSeekerProfile).Assembly,
    typeof(JobSeekerProfileModule).Assembly,
    typeof(Application).Assembly,
    typeof(JobApplicationModule).Assembly,
    typeof(JobIndexEntry).Assembly,
    typeof(SearchDiscoveryModule).Assembly,
    typeof(Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects.FactorWeights).Assembly,
    typeof(RecommendationEngineModule).Assembly,
    typeof(Partner).Assembly,
    typeof(ExternalJobSyncModule).Assembly,
    typeof(Nexhire.Modules.Reporting.Core.Domain.Aggregates.ReportDefinition).Assembly,
    typeof(ReportingModule).Assembly,
    typeof(Taxonomy).Assembly,
    typeof(AdministratorsConfigurationModuleExtensions).Assembly,
    typeof(Article).Assembly,
    typeof(ContentManagementModuleExtensions).Assembly,
    typeof(Nexhire.Modules.Notification.Domain.Aggregates.Notification).Assembly,
    typeof(Nexhire.Modules.Notification.Application.CQRS.Commands.SendImmediateNotificationCommand).Assembly,
    typeof(NotificationModule).Assembly
};

// 1. Register Shared Infrastructure layer
builder.Services.AddSharedInfrastructure(builder.Configuration, moduleAssemblies);

// 2. Register Active Modules (Injected as clean pluggable layers)
builder.Services.AddIdentityAccessModule(builder.Configuration);
builder.Services.AddEmployerProfilesModule(builder.Configuration);
builder.Services.AddJobPostingsModule(builder.Configuration);
builder.Services.AddJobSeekerProfileModule(builder.Configuration);
builder.Services.AddJobApplicationModule(builder.Configuration);
builder.Services.AddSearchDiscoveryModule(builder.Configuration);
builder.Services.AddRecommendationEngineModule(builder.Configuration);
builder.Services.AddExternalJobSyncModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddAdministratorsConfigurationModule(builder.Configuration);
builder.Services.AddContentManagementModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

builder.Services.AddScoped<IJobSeekerProfileQueryApi, JobSeekerProfileQueryApiAdapter>();
builder.Services.AddScoped<IIdentityProvisioningApi, IdentityProvisioningApiAdapter>();
builder.Services.AddScoped<ITokenValidationApi, TokenValidationApiAdapter>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => { });

var app = builder.Build();

// 3. Configure HTTP Pipeline and Middleware
app.UseOpenApiDocumentation();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseNexhireAuthentication();
app.UseAuthorization();

await app.Services.SeedIdentityAccessDataAsync();

// 4. Map Pluggable Module Routing
app.MapIdentityAccessEndpoints();
app.MapEmployerProfilesEndpoints();
app.MapJobPostingsEndpoints();
app.MapJobSeekerProfileEndpoints();
app.MapJobApplicationEndpoints();
app.MapSearchDiscoveryEndpoints();
app.MapRecommendationEngineEndpoints();
app.MapExternalJobSyncEndpoints();
app.MapReportingEndpoints();
app.MapAdministratorsConfigurationEndpoints();
app.MapContentManagementEndpoints();
app.MapNotificationEndpoints();

// Base System Health Endpoint
app.MapGet("health", () => Results.Ok(new 
{ 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow, 
    System = "Nexhire Modular Monolith" 
}))
.WithName("HealthCheck")
.WithTags("System");

app.Run();

public partial class Program { }
