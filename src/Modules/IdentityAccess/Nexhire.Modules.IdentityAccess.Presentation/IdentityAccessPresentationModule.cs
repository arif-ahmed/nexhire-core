using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.IdentityAccess.Presentation.Endpoints;

namespace Nexhire.Modules.IdentityAccess.Presentation;

public static class IdentityAccessPresentationModule
{
    public static IEndpointRouteBuilder MapIdentityAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        UserEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
