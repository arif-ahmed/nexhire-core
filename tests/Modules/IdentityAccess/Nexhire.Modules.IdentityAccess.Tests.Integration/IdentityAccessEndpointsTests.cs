using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;
using Xunit;

namespace Nexhire.Modules.IdentityAccess.Tests.Integration;

public class IdentityAccessEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IdentityAccessEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_Return_Success_When_Valid_Credentials()
    {
        // Arrange
        var command = new LoginWithCredentialsCommand(
            Identifier: "seeker@nexhire.com",
            Password: "Seeker@Password2025!",
            Channel: "Web",
            DeviceFingerprint: "test-device-123",
            IpAddress: "127.0.0.1"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/identity/login", command);

        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because it should log in, but got: " + content);
        
        content.Should().NotBeNullOrEmpty();
    }
}
