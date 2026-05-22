using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Infrastructure;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Infrastructure;
using Nexhire.Modules.Users.Infrastructure;
using Nexhire.Shared.Infrastructure;
using Nexhire.Shared.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Define active module assemblies for dynamic scanning (MediatR CQRS and FluentValidation schemas)
var moduleAssemblies = new[]
{
    typeof(Nexhire.Modules.Users.Core.Domain.User).Assembly,
    typeof(UsersModule).Assembly,
    typeof(EmployerProfile).Assembly,
    typeof(EmployerProfilesModule).Assembly,
    typeof(JobSeekerProfile).Assembly,
    typeof(JobSeekerProfileModule).Assembly
};

// 1. Register Shared Infrastructure layer
builder.Services.AddSharedInfrastructure(builder.Configuration, moduleAssemblies);

// 2. Register Active Modules (Injected as clean pluggable layers)
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddEmployerProfilesModule(builder.Configuration);
builder.Services.AddJobSeekerProfileModule(builder.Configuration);

var app = builder.Build();

// 3. Configure HTTP Pipeline and Middleware
app.UseOpenApiDocumentation();
app.UseHttpsRedirection();

// 4. Map Pluggable Module Routing
app.MapUsersEndpoints();
app.MapEmployerProfilesEndpoints();
app.MapJobSeekerProfileEndpoints();

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
