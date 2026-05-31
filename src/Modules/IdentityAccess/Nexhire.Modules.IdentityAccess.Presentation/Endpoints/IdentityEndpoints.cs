using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ActivateAccount;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ResendActivationOtp;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RefreshAccessToken;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RevokeToken;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RequestPasswordReset;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyPasswordResetOtp;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CompletePasswordReset;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.IssueOAuthToken;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.EnrollMfa;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ConfirmMfaEnrollment;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.DisableMfa;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyMfaChallenge;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.Logout;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LogoutAllSessions;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ChangePassword;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminApproveEmployer;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminRejectEmployer;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminSuspendUser;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminReinstateUser;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminDeactivateUser;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminUnlockAccount;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminIssuePasswordReset;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AssignRole;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMyAccount;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMySessions;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMfaStatus;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ListUsers;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetUserAsAdmin;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetAdminActionLog;

namespace Nexhire.Modules.IdentityAccess.Presentation.Endpoints;

public static class IdentityEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/identity").WithTags("Identity Access");

        // Anonymous endpoints
        group.MapPost("login", async (LoginWithCredentialsCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("activate", async (ActivateAccountCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("activate/resend", async (ResendActivationOtpCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("token/refresh", async (RefreshAccessTokenCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("token/revoke", async (RevokeTokenCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("password/reset-request", async (RequestPasswordResetCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("password/reset-verify", async (VerifyPasswordResetOtpCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("password/reset", async (CompletePasswordResetCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));
        group.MapPost("oauth/token", async ([AsParameters] IssueOAuthTokenCommand command, ISender sender) => ToHttpResult(await sender.Send(command)));

        // Authenticated endpoints (require valid access token)
        group.MapGet("me", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new GetMyAccountQuery(user.GetUserId())))).RequireAuthorization();
        group.MapGet("me/sessions", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new GetMySessionsQuery(user.GetUserId(), user.GetSessionId())))).RequireAuthorization();
        group.MapGet("me/mfa", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new GetMfaStatusQuery(user.GetUserId())))).RequireAuthorization();
        
        group.MapPost("mfa/enroll", async (ClaimsPrincipal user, [FromBody] EnrollMfaRequest req, ISender sender) => ToHttpResult(await sender.Send(new EnrollMfaCommand(user.GetUserId(), req.Method)))).RequireAuthorization();
        group.MapPost("mfa/enroll/confirm", async (ClaimsPrincipal user, [FromBody] ConfirmMfaEnrollmentRequest req, ISender sender) => ToHttpResult(await sender.Send(new ConfirmMfaEnrollmentCommand(user.GetUserId(), req.Code, req.Method)))).RequireAuthorization();
        group.MapDelete("mfa", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new DisableMfaCommand(user.GetUserId())))).RequireAuthorization();
        
        group.MapPost("mfa/verify", async ([FromBody] VerifyMfaChallengeRequest req, HttpContext context, ISender sender) => 
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return ToHttpResult(await sender.Send(new VerifyMfaChallengeCommand(req.ChallengeId, req.Code, req.Channel, req.DeviceFingerprint, ip)));
        }).RequireAuthorization();

        group.MapPost("logout", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new LogoutCommand(user.GetUserId(), user.GetSessionId())))).RequireAuthorization();
        group.MapPost("logout-all", async (ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new LogoutAllSessionsCommand(user.GetUserId())))).RequireAuthorization();
        group.MapPost("password/change", async (ClaimsPrincipal user, [FromBody] ChangePasswordRequest req, ISender sender) => ToHttpResult(await sender.Send(new ChangePasswordCommand(user.GetUserId(), req.CurrentPassword, req.NewPassword)))).RequireAuthorization();

        // Admin endpoints (require valid token + "users:manage" permission)
        group.MapGet("admin/users", async ([AsParameters] ListUsersQuery query, ISender sender) => ToHttpResult(await sender.Send(query)))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
            
        group.MapGet("admin/users/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new GetUserAsAdminQuery(user.GetUserId(), id))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
            
        group.MapPost("admin/users/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new AdminApproveEmployerCommand(user.GetUserId(), id))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/reject", async (Guid id, ClaimsPrincipal user, [FromBody] AdminRejectEmployerRequest req, ISender sender) => ToHttpResult(await sender.Send(new AdminRejectEmployerCommand(user.GetUserId(), id, req.Reason))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/suspend", async (Guid id, ClaimsPrincipal user, [FromBody] AdminSuspendUserRequest req, ISender sender) => ToHttpResult(await sender.Send(new AdminSuspendUserCommand(user.GetUserId(), id, req.Reason))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/reinstate", async (Guid id, ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new AdminReinstateUserCommand(user.GetUserId(), id))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/deactivate", async (Guid id, ClaimsPrincipal user, [FromBody] AdminDeactivateUserRequest req, ISender sender) => ToHttpResult(await sender.Send(new AdminDeactivateUserCommand(user.GetUserId(), id, req.Reason))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/unlock", async (Guid id, ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new AdminUnlockAccountCommand(user.GetUserId(), id))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/password-reset", async (Guid id, ClaimsPrincipal user, ISender sender) => ToHttpResult(await sender.Send(new AdminIssuePasswordResetCommand(user.GetUserId(), id))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapPost("admin/users/{id:guid}/role", async (Guid id, ClaimsPrincipal user, [FromBody] AssignRoleRequest req, ISender sender) => ToHttpResult(await sender.Send(new AssignRoleCommand(user.GetUserId(), id, req.Role))))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        
        group.MapGet("admin/audit", async ([AsParameters] GetAdminActionLogQuery query, ISender sender) => ToHttpResult(await sender.Send(query)))
            .RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
    }

    private static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idString = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    private static Guid GetSessionId(this ClaimsPrincipal principal)
    {
        var idString = principal.FindFirstValue("session_id");
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    private static IResult ToHttpResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return result.Error.Code switch
        {
            string c when c.Contains("INVALID") || c.Contains("MISSING") || c.Contains("EMPTY") => Results.BadRequest(result.Error),
            string c when c.Contains("UNAUTHORIZED") => Results.Unauthorized(),
            string c when c.Contains("FORBIDDEN") || c.Contains("BANNED") => Results.Json(result.Error, statusCode: 403),
            string c when c.Contains("NOT-FOUND") => Results.NotFound(result.Error),
            string c when c.Contains("EXPIRED") => Results.Json(result.Error, statusCode: 410),
            string c when c.Contains("CONFLICT") || c.Contains("DUPLICATE") || c.Contains("ALREADY") => Results.Json(result.Error, statusCode: 409),
            string c when c.Contains("LOCKED") => Results.Json(result.Error, statusCode: 423),
            string c when c.Contains("RATE-LIMITED") => Results.Json(result.Error, statusCode: 429),
            _ => Results.BadRequest(result.Error)
        };
    }
    
    private static IResult ToHttpResult(Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return result.Error.Code switch
        {
            string c when c.Contains("INVALID") || c.Contains("MISSING") || c.Contains("EMPTY") => Results.BadRequest(result.Error),
            string c when c.Contains("UNAUTHORIZED") => Results.Unauthorized(),
            string c when c.Contains("FORBIDDEN") || c.Contains("BANNED") => Results.Json(result.Error, statusCode: 403),
            string c when c.Contains("NOT-FOUND") => Results.NotFound(result.Error),
            string c when c.Contains("EXPIRED") => Results.Json(result.Error, statusCode: 410),
            string c when c.Contains("CONFLICT") || c.Contains("DUPLICATE") || c.Contains("ALREADY") => Results.Json(result.Error, statusCode: 409),
            string c when c.Contains("LOCKED") => Results.Json(result.Error, statusCode: 423),
            string c when c.Contains("RATE-LIMITED") => Results.Json(result.Error, statusCode: 429),
            _ => Results.BadRequest(result.Error)
        };
    }
}

public record EnrollMfaRequest(string Method);
public record ConfirmMfaEnrollmentRequest(string Code, string Method);
public record VerifyMfaChallengeRequest(Guid ChallengeId, string Code, string Channel, string DeviceFingerprint);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminRejectEmployerRequest(string Reason);
public record AdminSuspendUserRequest(string Reason);
public record AdminDeactivateUserRequest(string Reason);
public record AssignRoleRequest(string Role);
