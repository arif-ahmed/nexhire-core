using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Contracts;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Infrastructure.PublicApi;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class EmployerProfilePublicApiAdapterTests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly EmployerProfilePublicApiAdapter _adapter;

    public EmployerProfilePublicApiAdapterTests()
    {
        _adapter = new EmployerProfilePublicApiAdapter(_repository);
    }

    [Fact]
    public async Task IsVerifiedAsync_ReturnsFalse_WhenProfileNotFound()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((EmployerProfile?)null);

        var result = await _adapter.IsVerifiedAsync(userId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsVerifiedAsync_ReturnsTrue_WhenProfileIsVerified()
    {
        var userId = Guid.NewGuid();
        var profile = CreateVerifiedProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _adapter.IsVerifiedAsync(userId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsNull_WhenProfileNotFound()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((EmployerProfile?)null);

        var result = await _adapter.GetSummaryAsync(userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_MapsAllFields_WhenProfileFound()
    {
        var userId = Guid.NewGuid();
        var profile = CreateVerifiedProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _adapter.GetSummaryAsync(userId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.CompanyName.Should().Be("Nexhire Corp");
        result.IsVerified.Should().BeTrue();
        result.Status.Should().Be("Verified");
    }

    private static EmployerProfile CreateVerifiedProfile(Guid userId)
    {
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Corp").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create($"REG{Guid.NewGuid():N}").Value);

        profile.Activate();

        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        var companySize = CompanySize.Create(CompanySizeEnum.Medium).Value;
        profile.CompleteLevel2(website, "Technology", companySize, address, desc);

        profile.BeginAutomaticVerification("REG-REF-001");
        profile.RecordAutomaticVerificationPassed("GOV-EVIDENCE-PASS");

        return profile;
    }
}
