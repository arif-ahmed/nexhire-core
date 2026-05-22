using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Infrastructure.Endpoints;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.Endpoints;

public class JobApplicationEndpointTests
{
    [Fact]
    public void MapEndpoints_ShouldRegisterAllRequiredJobApplicationEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        var senderMock = Substitute.For<ISender>();
        var idempotencyMock = Substitute.For<IIdempotencyKeyStore>();
        services.AddSingleton(senderMock);
        services.AddSingleton(idempotencyMock);
        var serviceProvider = services.BuildServiceProvider();

        var routeBuilder = Substitute.For<IEndpointRouteBuilder>();
        var dataSources = new List<EndpointDataSource>();
        routeBuilder.DataSources.Returns(dataSources);
        routeBuilder.ServiceProvider.Returns(serviceProvider);
        routeBuilder.CreateApplicationBuilder().Returns(new ApplicationBuilder(serviceProvider));

        // Act
        JobApplicationEndpoints.MapEndpoints(routeBuilder);

        // Assert
        var endpoints = dataSources
            .SelectMany(ds => ds.Endpoints)
            .Cast<RouteEndpoint>()
            .ToList();

        endpoints.Should().NotBeEmpty("Endpoints should be registered");

        var routes = endpoints.Select(e => new
        {
            Pattern = e.RoutePattern.RawText,
            Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.FirstOrDefault()
        }).ToList();

        // Bookmarks Endpoints
        routes.Should().Contain(r => (r.Pattern == "api/bookmarks" || r.Pattern == "api/bookmarks/") && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/bookmarks/{postingId:guid}" && r.Method == "DELETE");
        routes.Should().Contain(r => (r.Pattern == "api/bookmarks" || r.Pattern == "api/bookmarks/") && r.Method == "GET");

        // Applications Endpoints
        routes.Should().Contain(r => (r.Pattern == "api/applications" || r.Pattern == "api/applications/") && r.Method == "POST");
        routes.Should().Contain(r => r.Pattern == "api/applications/{applicationId:guid}/withdraw" && r.Method == "POST");
        routes.Should().Contain(r => (r.Pattern == "api/applications" || r.Pattern == "api/applications/") && r.Method == "GET");
        routes.Should().Contain(r => r.Pattern == "api/applications/{applicationId:guid}" && r.Method == "GET");
    }
}
