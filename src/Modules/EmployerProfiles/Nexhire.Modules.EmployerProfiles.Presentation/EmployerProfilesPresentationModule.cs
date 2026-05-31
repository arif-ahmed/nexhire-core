using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.EmployerProfiles.Presentation.Endpoints;

namespace Nexhire.Modules.EmployerProfiles.Presentation;

public static class EmployerProfilesPresentationModule
{
    public static IEndpointRouteBuilder MapEmployerProfilesEndpoints(this IEndpointRouteBuilder app)
    {
        EmployerEndpoints.MapEndpoints(app);
        return app;
    }
}
