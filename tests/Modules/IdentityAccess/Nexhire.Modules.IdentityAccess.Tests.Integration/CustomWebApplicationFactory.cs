using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Nexhire.Modules.IdentityAccess.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to "Testing" to avoid picking up dev/prod config accidentally
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // If you are using a real database, you might remove the existing DbContext configuration here
            // and replace it with an In-Memory database or a Testcontainers configuration.
            
            // Example: Replacing IdentityAccessDbContext with an explicitly isolated In-Memory db for testing
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityAccessDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbName = $"IdentityAccess_IntegrationTests_{Guid.NewGuid()}";
            
            services.AddDbContext<IdentityAccessDbContext>((sp, options) =>
            {
                // We use a unique database name per factory instance
                options.UseInMemoryDatabase(dbName);
                
                // Keep the domain event interceptor for tests to remain realistic!
                var interceptor = sp.GetService<Nexhire.Shared.Infrastructure.Interceptors.PublishDomainEventsInterceptor>();
                if (interceptor != null)
                {
                    options.AddInterceptors(interceptor);
                }
            });
            
            // We can also mock external dependencies here, e.g., third-party email providers
        });
    }
}
