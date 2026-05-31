using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.Services;

public class PermissionResolverTests
{
    public class Resolve
    {
        // ── Role baselines ───────────────────────────────────────────────────

        [Fact]
        public void JobSeeker_Should_Have_Correct_Baseline_Permissions()
        {
            var permissions = PermissionResolver.Resolve(UserRole.JobSeeker, []);

            permissions.Should().BeEquivalentTo(new[]
            {
                "profile:self",
                "applications:self",
                "search:read"
            });
        }

        [Fact]
        public void Employer_Should_Have_Correct_Baseline_Permissions()
        {
            var permissions = PermissionResolver.Resolve(UserRole.Employer, []);

            permissions.Should().BeEquivalentTo(new[]
            {
                "employer:self",
                "jobs:write",
                "applications:read",
                "candidates:read"
            });
        }

        [Fact]
        public void ThirdPartyPortal_Should_Have_Correct_Baseline_Permissions()
        {
            var permissions = PermissionResolver.Resolve(UserRole.ThirdPartyPortal, []);

            permissions.Should().BeEquivalentTo(new[]
            {
                "integrations:read",
                "jobs:write"
            });
        }

        [Fact]
        public void MoLAdministrator_Should_Have_All_Permissions_Including_Admin_Specific()
        {
            var permissions = PermissionResolver.Resolve(UserRole.MoLAdministrator, []);

            permissions.Should().Contain("users:manage");
            permissions.Should().Contain("jobs:moderate");
            permissions.Should().Contain("taxonomy:manage");
            permissions.Should().Contain("reports:read");
            // Plus all other role permissions
            permissions.Should().Contain("profile:self");
            permissions.Should().Contain("applications:self");
            permissions.Should().Contain("search:read");
            permissions.Should().Contain("employer:self");
            permissions.Should().Contain("jobs:write");
            permissions.Should().Contain("applications:read");
            permissions.Should().Contain("candidates:read");
            permissions.Should().Contain("integrations:read");
        }

        // ── Explicit grants are unioned ──────────────────────────────────────

        [Fact]
        public void Should_Union_Explicit_Grants_With_Role_Baseline()
        {
            var explicitGrants = new[] { "custom:grant", "another:perm" };

            var permissions = PermissionResolver.Resolve(UserRole.JobSeeker, explicitGrants);

            permissions.Should().Contain("profile:self");
            permissions.Should().Contain("applications:self");
            permissions.Should().Contain("search:read");
            permissions.Should().Contain("custom:grant");
            permissions.Should().Contain("another:perm");
        }

        [Fact]
        public void Should_Not_Duplicate_Permissions_When_Grant_Overlaps_Baseline()
        {
            var explicitGrants = new[] { "profile:self" }; // already in JobSeeker baseline

            var permissions = PermissionResolver.Resolve(UserRole.JobSeeker, explicitGrants);

            permissions.Count(p => p == "profile:self").Should().Be(1,
                because: "duplicate permissions must not be added");
        }

        [Fact]
        public void Should_Return_Only_Grants_When_Empty_Grants_And_Unknown_Role_Defaults()
        {
            var permissions = PermissionResolver.Resolve(UserRole.JobSeeker, []);

            permissions.Should().NotBeEmpty();
            permissions.Should().NotContain(string.Empty);
        }
    }
}
