using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Infrastructure.Interceptors;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit;

public class PersistenceTests
{
    private readonly EmployerProfilesDbContext _dbContext;
    private readonly IPublisher _publisherMock;

    public PersistenceTests()
    {
        _publisherMock = Substitute.For<IPublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(_publisherMock);
        var interceptor = new PublishDomainEventsInterceptor(services.BuildServiceProvider());

        var options = new DbContextOptionsBuilder<EmployerProfilesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EmployerProfilesDbContext(options, interceptor);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveAndRetrieveEmployerProfile_ShouldMapAllFieldsAndCollectionsCorrectly()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var companyName = CompanyName.Create("Acme Corp").Value;
        var email = EmailAddress.Create("acme@example.com").Value;
        var mobile = MobileNumber.Create("+8801700000000").Value;
        var companyIdentifier = CompanyIdentifier.Create("REG123456").Value;

        var profile = EmployerProfile.Register(profileId, userId, companyName, email, mobile, companyIdentifier);

        // Add optional L2 details
        var website = WebsiteUrl.Create("https://acme.com").Value;
        var address = Address.Create("Street 1", "Floor 2", "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var description = CompanyDescription.Create("Super company").Value;
        profile.Activate();
        profile.CompleteLevel2(website, "Technology", CompanySize.Create(CompanySizeEnum.Medium).Value, address, description);

        // Add logo & images & documents
        var logoFile = FileReference.Create("logos/acme.png", "acme.png", "image/png", 1024).Value;
        var cleanScan = VirusScanResult.Create(VirusScanStatus.Clean, DateTime.UtcNow).Value;
        profile.SetLogo(logoFile, cleanScan);

        var imageFile = FileReference.Create("images/office.png", "office.png", "image/png", 2048).Value;
        profile.AddCompanyImage(imageFile, cleanScan);

        var docFile = FileReference.Create("docs/vat.pdf", "vat.pdf", "application/pdf", 4096).Value;
        profile.AddSupplementaryDocument(docFile, DocumentKind.VatCertificate, cleanScan);

        var repository = new EmployerProfileRepository(_dbContext);
        var unitOfWork = new UnitOfWork(_dbContext);

        // Act
        await repository.AddAsync(profile);
        await unitOfWork.SaveChangesAsync();

        // Clear tracker to force reload
        _dbContext.ChangeTracker.Clear();

        var retrievedProfile = await repository.GetByIdAsync(profileId);

        // Assert
        retrievedProfile.Should().NotBeNull();
        retrievedProfile!.Id.Should().Be(profileId);
        retrievedProfile.UserId.Should().Be(userId);
        retrievedProfile.CompanyName.Value.Should().Be("Acme Corp");
        retrievedProfile.Email.Value.Should().Be("acme@example.com");
        retrievedProfile.Mobile.Value.Should().Be("+8801700000000");
        retrievedProfile.CompanyIdentifier.Value.Should().Be("REG123456");
        retrievedProfile.Website!.Value.Should().Be("https://acme.com");
        retrievedProfile.Industry.Should().Be("Technology");
        retrievedProfile.CompanySize.Should().NotBeNull();
        retrievedProfile.CompanySize!.Value.Should().Be(CompanySizeEnum.Medium);
        retrievedProfile.Description!.Value.Should().Be("Super company");

        retrievedProfile.Address.Should().NotBeNull();
        retrievedProfile.Address!.Line1.Should().Be("Street 1");
        retrievedProfile.Address!.Line2.Should().Be("Floor 2");
        retrievedProfile.Address!.City.Should().Be("Dhaka");
        retrievedProfile.Address!.District.Should().Be("Dhaka");
        retrievedProfile.Address!.Postcode.Should().Be("1212");
        retrievedProfile.Address!.Country.Should().Be("Bangladesh");

        retrievedProfile.Logo.Should().NotBeNull();
        retrievedProfile.Logo!.StorageKey.Should().Be("logos/acme.png");

        retrievedProfile.Images.Should().HaveCount(1);
        retrievedProfile.Images.First().File.StorageKey.Should().Be("images/office.png");

        retrievedProfile.Documents.Should().HaveCount(1);
        retrievedProfile.Documents.First().File.StorageKey.Should().Be("docs/vat.pdf");
        retrievedProfile.Documents.First().Kind.Should().Be(DocumentKind.VatCertificate);

        // Interceptor should have triggered publishing of domain events
        await _publisherMock.Received().Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAndRetrieveShortlist_ShouldMapAllFieldsAndMembersCorrectly()
    {
        // Arrange
        var shortlistId = Guid.NewGuid();
        var employerProfileId = Guid.NewGuid();
        var shortlistResult = Shortlist.Create(shortlistId, employerProfileId, "Top Developers");
        var shortlist = shortlistResult.Value;

        var candidateUserId = Guid.NewGuid();
        shortlist.AddCandidate(candidateUserId, 95);

        var repository = new ShortlistRepository(_dbContext);
        var unitOfWork = new UnitOfWork(_dbContext);

        // Act
        await repository.AddAsync(shortlist);
        await unitOfWork.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var retrievedShortlist = await repository.GetByIdAsync(shortlistId);

        // Assert
        retrievedShortlist.Should().NotBeNull();
        retrievedShortlist!.Id.Should().Be(shortlistId);
        retrievedShortlist.EmployerProfileId.Should().Be(employerProfileId);
        retrievedShortlist.Name.Should().Be("Top Developers");
        retrievedShortlist.IsDeleted.Should().BeFalse();

        retrievedShortlist.Members.Should().HaveCount(1);
        retrievedShortlist.Members.First().CandidateUserId.Should().Be(candidateUserId);
        retrievedShortlist.Members.First().MatchScore.Should().Be(95);
    }

    [Fact]
    public async Task DashboardProjectionStore_ShouldUpsertAndRetrieveProjectionsCorrectly()
    {
        // Arrange
        var store = new DashboardProjectionStore(_dbContext);
        var employerUserId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var matchedCandidateId = Guid.NewGuid();

        var posting = new DashboardPosting
        {
            PostingId = postingId,
            EmployerUserId = employerUserId,
            Title = "Senior C# Engineer",
            Status = "Active",
            LastEventOnUtc = DateTime.UtcNow
        };

        var application = new DashboardApplication
        {
            ApplicationId = applicationId,
            EmployerUserId = employerUserId,
            PostingId = postingId,
            JobSeekerId = Guid.NewGuid(),
            SubmittedOnUtc = DateTime.UtcNow
        };

        var matched = new DashboardMatchedCandidate
        {
            Id = matchedCandidateId,
            EmployerUserId = employerUserId,
            PostingId = postingId,
            CandidateUserId = Guid.NewGuid(),
            MatchScore = 88,
            GeneratedOnUtc = DateTime.UtcNow
        };

        // Act
        await store.UpsertPostingAsync(posting);
        await store.AddApplicationAsync(application);
        await store.UpsertMatchedCandidateAsync(matched);

        _dbContext.ChangeTracker.Clear();

        var postings = await store.GetPostingsAsync(employerUserId);
        var applications = await store.GetApplicationsAsync(employerUserId);
        var matches = await store.GetMatchedCandidatesAsync(employerUserId);

        // Assert
        postings.Should().HaveCount(1);
        postings.First().PostingId.Should().Be(postingId);
        postings.First().Title.Should().Be("Senior C# Engineer");

        applications.Should().HaveCount(1);
        applications.First().ApplicationId.Should().Be(applicationId);

        matches.Should().HaveCount(1);
        matches.First().PostingId.Should().Be(postingId);
        matches.First().MatchScore.Should().Be(88);
    }
}
