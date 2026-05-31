using System.Security.Claims;
using Nexhire.Modules.IdentityAccess.Contracts;

namespace Nexhire.Api.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidationApi tokenValidationApi)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = headerValue.Substring("Bearer ".Length).Trim();
                var result = await tokenValidationApi.Validate(token);
                
                if (result.IsSuccess)
                {
                    var principal = result.Value;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, principal.UserId.ToString()),
                        new Claim("sub", principal.UserId.ToString()),
                        new Claim(ClaimTypes.Role, principal.Role),
                        new Claim("session_id", principal.SessionId.ToString())
                    };
                    
                    foreach (var permission in principal.Permissions)
                    {
                        claims.Add(new Claim("permission", permission));
                    }
                    
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    context.User = new ClaimsPrincipal(identity);
                }
                else
                {
                    // Token is provided but invalid => 401
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(result.Error);
                    return; // Short-circuit
                }
            }
        }

        await _next(context);
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseNexhireAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
