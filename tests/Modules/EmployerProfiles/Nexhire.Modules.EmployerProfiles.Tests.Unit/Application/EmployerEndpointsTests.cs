using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Endpoints;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class EmployerEndpointsTests
{
    [Fact]
    public void MapEndpoints_ShouldRegisterAllRequiredEmployerEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        var senderMock = Substitute.For<ISender>();
        services.AddSingleton(senderMock);
        var serviceProvider = services.BuildServiceProvider();

        var routeBuilder = Substitute.For<IEndpointRouteBuilder>();
        var dataSources = new List<EndpointDataSource>();
        routeBuilder.DataSources.Returns(dataSources);
        routeBuilder.ServiceProvider.Returns(serviceProvider);
        routeBuilder.CreateApplicationBuilder().Returns(new ApplicationBuilder(serviceProvider));

        // Act
        EmployerEndpoints.MapEndpoints(routeBuilder);

        // Assert
        var endpoints = dataSources
            .SelectMany(ds => ds.Endpoints)
            .Cast<RouteEndpoint>()
            .ToList();

        endpoints.Should().NotBeEmpty("Endpoints should be registered");

        // Verify registration of key routes
        var routes = endpoints.Select(e => new 
        { 
            Pattern = e.RoutePattern.RawText, 
            Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.FirstOrDefault() 
        }).ToList();

        // 1. Anonymous Registration (GAP-8: renamed to /register)
        routes.Should().Contain(r => r.Pattern == "api/employers/register" && r.Method == "POST");

        // 2. Authenticated Profile & Status (GAP-8: routes aligned to spec)
        routes.Should().Contain(r => r.Pattern == "api/employers/me" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me" && r.Method == "PUT");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/level2" && r.Method == "PUT");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/verification/request" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/verification/resubmit" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/verification" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/dashboard" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/dashboard/postings" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/matched-candidates" && r.Method == "GET");

        // 3. Media & Uploads
        routes.Should().Contain(r => r.Pattern == "api/employers/me/logo" && r.Method == "PUT");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/images" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/images/{imageId:guid}" && r.Method == "DELETE");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/documents" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/documents/{documentId:guid}" && r.Method == "DELETE");

        // 4. Shortlists
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists/{shortlistId:guid}" && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists/{shortlistId:guid}" && r.Method == "PUT");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists/{shortlistId:guid}" && r.Method == "DELETE");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists/{shortlistId:guid}/candidates" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/me/shortlists/{shortlistId:guid}/candidates/{memberId:guid}" && r.Method == "DELETE");

        // 5. Public Profile (GAP-8: renamed to /public)
        routes.Should().Contain(r => r.Pattern == "api/employers/{id:guid}/public" && r.Method == "GET");

        // 6. Admin operations (GAP-8: routes aligned to spec)
        routes.Should().Contain(r => r.Pattern == "api/employers/{id:guid}/verification/approve" && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/employers/{id:guid}/verification/reject" && r.Method == "POST");
    }
}
