using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Endpoints;

public static class ContentManagementEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/content")
            .WithTags("Content Management");

        // Health / probe
        group.MapGet("health", () => Results.Ok(new { Status = "ContentManagement module loaded" }))
            .WithName("ContentManagementHealth")
            .AllowAnonymous();

        // Full endpoint wiring will be completed as commands/queries are implemented.
        // This provides the module registration contract so the host compiles.
    }

    private static IResult ToHttp(Result result) => result.IsSuccess
        ? Results.Ok()
        : result.Error.Code switch
        {
            "E-ARTICLE-NO-CATEGORY" => Results.Conflict(result.Error),
            "E-ARTICLE-ILLEGAL-TRANSITION" => Results.Conflict(result.Error),
            "E-SCHEDULE-PAST" => Results.BadRequest(result.Error),
            "E-CATEGORY-IN-USE" => Results.Conflict(result.Error),
            "E-TOPIC-IN-USE" => Results.Conflict(result.Error),
            "E-BULK-LIMIT-EXCEEDED" => Results.BadRequest(result.Error),
            "E-MEDIA-INVALID-FORMAT" => Results.BadRequest(result.Error),
            "E-MEDIA-SIZE-EXCEEDED" => Results.StatusCode(413),
            _ => Results.BadRequest(result.Error)
        };

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess) => result.IsSuccess
        ? onSuccess(result.Value)
        : ToHttp(result);
}
