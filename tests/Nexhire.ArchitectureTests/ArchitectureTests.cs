using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace Nexhire.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Assembly CoreAssembly = typeof(Nexhire.Modules.Users.Core.Domain.User).Assembly;

    [Fact]
    public void UsersCore_Should_Not_DependOn_UsersInfrastructure_Or_Api()
    {
        // Arrange
        var forbiddenDependencies = new[]
        {
            "Nexhire.Modules.Users.Infrastructure",
            "Nexhire.Shared.Infrastructure",
            "Nexhire.Api"
        };

        // Act
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "The Domain & Core layer of the Users module has disallowed references to Infrastructure or Host API projects.");
    }
}
