using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.Users.Core.Domain.Repositories;
using Nexhire.Modules.Users.Infrastructure.Endpoints;
using Nexhire.Modules.Users.Infrastructure.Persistence;
using Nexhire.Modules.Users.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.Users.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        UserEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
