using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.Users.Core.Domain;
using Nexhire.Shared.Infrastructure.Interceptors;

namespace Nexhire.Modules.Users.Infrastructure.Persistence;

public class UsersDbContext : DbContext
{
    private readonly PublishDomainEventsInterceptor _domainEventsInterceptor;

    public UsersDbContext(
        DbContextOptions<UsersDbContext> options,
        PublishDomainEventsInterceptor domainEventsInterceptor) : base(options)
    {
        _domainEventsInterceptor = domainEventsInterceptor;
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Segregate this module's data into its own logical schema
        modelBuilder.HasDefaultSchema("users");
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
