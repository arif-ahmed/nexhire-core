using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CreateUser;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetUserById;

namespace Nexhire.Modules.IdentityAccess.Presentation.Endpoints;

public static class UserEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users")
            .WithTags("Identity Access");

        group.MapPost("", async (CreateUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/users/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateUser")
        .WithSummary("Creates a new user account")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<Shared.Core.Results.Error>(StatusCodes.Status400BadRequest);

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetUserByIdQuery(id));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        })
        .WithName("GetUserById")
        .WithSummary("Retrieves a user account by their unique ID")
        .Produces<Application.DTOs.UserDto>(StatusCodes.Status200OK)
        .Produces<Shared.Core.Results.Error>(StatusCodes.Status404NotFound);
    }
}
