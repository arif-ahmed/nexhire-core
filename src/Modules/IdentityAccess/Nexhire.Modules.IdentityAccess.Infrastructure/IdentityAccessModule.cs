using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.IdentityAccess.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.IdentityAccess.Infrastructure;

public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();

        return services;
    }
}
