using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Modules.ContentManagement.Infrastructure.Background;
using Nexhire.Modules.ContentManagement.Infrastructure.Endpoints;
using Nexhire.Modules.ContentManagement.Infrastructure.IntegrationEvents.Publishers;
using Nexhire.Modules.ContentManagement.Infrastructure.Persistence;
using Nexhire.Modules.ContentManagement.Infrastructure.Persistence.Repositories;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Startup;

public static class ContentManagementModuleExtensions
{
    public static IServiceCollection AddContentManagementModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ContentManagementDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFaqEntryRepository, FaqEntryRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IGuidedTourRepository, GuidedTourRepository>();
        services.AddScoped<IContentPreferenceRepository, ContentPreferenceRepository>();
        services.AddScoped<IHelpFeedbackRepository, HelpFeedbackRepository>();
        services.AddScoped<IContentManagementUnitOfWork>(sp => sp.GetRequiredService<ContentManagementDbContext>());

        services.AddHostedService<ScheduledPublicationWorker>();
        services.AddHostedService<ContentManagementOutboxRelayBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapContentManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ContentManagementEndpoints.MapEndpoints(endpoints);
        return endpoints;
    }
}
