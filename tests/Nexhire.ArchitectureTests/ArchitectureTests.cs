using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace Nexhire.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Assembly CoreAssembly = typeof(Nexhire.Modules.IdentityAccess.Domain.Domain.UserAccount).Assembly;
    private static readonly Assembly JobPostingsCoreAssembly = typeof(Nexhire.Modules.JobPostings.Core.Domain.Aggregates.JobPosting).Assembly;

    [Fact]
    public void IdentityAccessDomain_Should_Not_DependOn_Infrastructure_Or_Api()
    {
        // Arrange
        var forbiddenDependencies = new[]
        {
            "Nexhire.Modules.IdentityAccess.Infrastructure",
            "Nexhire.Shared.Infrastructure",
            "Nexhire.Api"
        };

        // Act
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "The Domain layer of the IdentityAccess module has disallowed references to Infrastructure or Host API projects.");
    }

    [Fact]
    public void JobPostingsCore_Should_Not_DependOn_JobPostingsInfrastructure_Or_Api()
    {
        var forbiddenDependencies = new[]
        {
            "Nexhire.Modules.JobPostings.Infrastructure",
            "Nexhire.Shared.Infrastructure",
            "Nexhire.Api"
        };

        var result = Types.InAssembly(JobPostingsCoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, "The JobPostings Core layer has disallowed references to Infrastructure or Host API projects.");
    }
}
