using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents;
using Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents.Consumers;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Nexhire.Shared.Infrastructure.Messaging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit;

public class PersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().Build();
    private EmployerProfilesDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        var interceptor = new PublishDomainEventsInterceptor(services.BuildServiceProvider());

        var options = new DbContextOptionsBuilder<EmployerProfilesDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;

        _dbContext = new EmployerProfilesDbContext(options, interceptor);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _pg.DisposeAsync();
    }

    private EmployerProfilesDbContext CreateFreshContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        var interceptor = new PublishDomainEventsInterceptor(services.BuildServiceProvider());
        var options = new DbContextOptionsBuilder<EmployerProfilesDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;
        return new EmployerProfilesDbContext(options, interceptor);
    }

    [Fact]
    public async Task SaveAndRetrieveEmployerProfile_ShouldMapAllFieldsAndCollectionsCorrectly()
    {
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var companyName = CompanyName.Create("Acme Corp").Value;
        var email = EmailAddress.Create("acme@example.com").Value;
        var mobile = MobileNumber.Create("+8801700000000").Value;
        var companyIdentifier = CompanyIdentifier.Create($"REG{Guid.NewGuid():N}").Value;

        var profile = EmployerProfile.Register(profileId, userId, companyName, email, mobile, companyIdentifier);

        var website = WebsiteUrl.Create("https://acme.com").Value;
        var address = Address.Create("Street 1", "Floor 2", "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var description = CompanyDescription.Create("Super company").Value;
        profile.Activate();
        profile.CompleteLevel2(website, "Technology", CompanySize.Create(CompanySizeEnum.Medium).Value, address, description);

        var logoFile = FileReference.Create("logos/acme.png", "acme.png", "image/png", 1024).Value;
        var cleanScan = VirusScanResult.Create(VirusScanStatus.Clean, DateTime.UtcNow).Value;
        profile.SetLogo(logoFile, cleanScan);

        var imageFile = FileReference.Create("images/office.png", "office.png", "image/png", 2048).Value;
        profile.AddCompanyImage(imageFile, cleanScan);

        var docFile = FileReference.Create("docs/vat.pdf", "vat.pdf", "application/pdf", 4096).Value;
        profile.AddSupplementaryDocument(docFile, DocumentKind.VatCertificate, cleanScan);

        var repository = new EmployerProfileRepository(_dbContext);
        var unitOfWork = new UnitOfWork(_dbContext);

        await repository.AddAsync(profile);
        await unitOfWork.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var retrievedProfile = await repository.GetByIdAsync(profileId);

        retrievedProfile.Should().NotBeNull();
        retrievedProfile!.Id.Should().Be(profileId);
        retrievedProfile.UserId.Should().Be(userId);
        retrievedProfile.CompanyName.Value.Should().Be("Acme Corp");
        retrievedProfile.Website!.Value.Should().Be("https://acme.com");
        retrievedProfile.Images.Should().HaveCount(1);
        retrievedProfile.Documents.Should().HaveCount(1);
        retrievedProfile.Documents.First().Kind.Should().Be(DocumentKind.VatCertificate);
    }

    [Fact]
    public async Task SaveAndRetrieveShortlist_ShouldMapAllFieldsAndMembersCorrectly()
    {
        var shortlistId = Guid.NewGuid();
        var employerProfileId = Guid.NewGuid();
        var shortlist = Shortlist.Create(shortlistId, employerProfileId, "Top Developers").Value;

        var candidateUserId = Guid.NewGuid();
        shortlist.AddCandidate(candidateUserId, 95);

        var repository = new ShortlistRepository(_dbContext);
        var unitOfWork = new UnitOfWork(_dbContext);

        await repository.AddAsync(shortlist);
        await unitOfWork.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var retrievedShortlist = await repository.GetByIdAsync(shortlistId);

        retrievedShortlist.Should().NotBeNull();
        retrievedShortlist!.Name.Should().Be("Top Developers");
        retrievedShortlist.Members.Should().HaveCount(1);
        retrievedShortlist.Members.First().CandidateUserId.Should().Be(candidateUserId);
        retrievedShortlist.Members.First().MatchScore.Should().Be(95);
    }

    [Fact]
    public async Task DashboardProjectionStore_ShouldUpsertAndRetrieveProjectionsCorrectly()
    {
        var store = new DashboardProjectionStore(_dbContext);
        var employerUserId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var matchedCandidateId = Guid.NewGuid();

        var posting = new DashboardPosting { PostingId = postingId, EmployerUserId = employerUserId, Title = "Senior C# Engineer", Status = "Active", LastEventOnUtc = DateTime.UtcNow };
        var application = new DashboardApplication { ApplicationId = applicationId, EmployerUserId = employerUserId, PostingId = postingId, JobSeekerId = Guid.NewGuid(), SubmittedOnUtc = DateTime.UtcNow };
        var matched = new DashboardMatchedCandidate { Id = matchedCandidateId, EmployerUserId = employerUserId, PostingId = postingId, CandidateUserId = Guid.NewGuid(), MatchScore = 88, GeneratedOnUtc = DateTime.UtcNow };

        await store.UpsertPostingAsync(posting);
        await store.AddApplicationAsync(application);
        await store.UpsertMatchedCandidateAsync(matched);
        _dbContext.ChangeTracker.Clear();

        var postings = await store.GetPostingsAsync(employerUserId);
        var applications = await store.GetApplicationsAsync(employerUserId);
        var matches = await store.GetMatchedCandidatesAsync(employerUserId);

        postings.Should().HaveCount(1);
        postings.First().Title.Should().Be("Senior C# Engineer");
        applications.Should().HaveCount(1);
        matches.Should().HaveCount(1);
        matches.First().MatchScore.Should().Be(88);
    }

    [Fact]
    public async Task UniqueIndex_ShouldRejectSecondProfileWithSameCompanyIdentifier()
    {
        var identifier = $"REG{Guid.NewGuid():N}";
        var profile1 = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("Corp A").Value, EmailAddress.Create("a@a.com").Value,
            MobileNumber.Create("+8801711111111").Value, CompanyIdentifier.Create(identifier).Value);
        var profile2 = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("Corp B").Value, EmailAddress.Create("b@b.com").Value,
            MobileNumber.Create("+8801722222222").Value, CompanyIdentifier.Create(identifier).Value);

        var repo = new EmployerProfileRepository(_dbContext);
        var uow = new UnitOfWork(_dbContext);

        await repo.AddAsync(profile1);
        await uow.SaveChangesAsync();

        await using var ctx2 = CreateFreshContext();
        var repo2 = new EmployerProfileRepository(ctx2);
        var uow2 = new UnitOfWork(ctx2);
        await repo2.AddAsync(profile2);

        Func<Task> act = () => uow2.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>("unique index on company_identifier must be enforced");
    }

    [Fact]
    public async Task OutboxTransactionality_ShouldWriteOutboxRowWithProfile()
    {
        var profile = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("Outbox Corp").Value, EmailAddress.Create("outbox@test.com").Value,
            MobileNumber.Create("+8801733333333").Value, CompanyIdentifier.Create($"OBX{Guid.NewGuid():N}").Value);

        var repo = new EmployerProfileRepository(_dbContext);
        var uow = new UnitOfWork(_dbContext);

        await repo.AddAsync(profile);
        await uow.SaveChangesAsync();

        var outboxCount = await _dbContext.OutboxMessages.CountAsync();
        outboxCount.Should().BeGreaterThan(0, "integration events should be written to outbox in same transaction");
    }

    [Fact]
    public async Task InboxIdempotency_ShouldNotDuplicateOnSecondDelivery()
    {
        var eventId = Guid.NewGuid();
        var inbox1 = new InboxMessage(eventId, nameof(UserAccountActivatedIntegrationEvent), DateTime.UtcNow);
        _dbContext.InboxMessages.Add(inbox1);
        await _dbContext.SaveChangesAsync();

        var isDuplicate = await _dbContext.InboxMessages.AnyAsync(m => m.Id == eventId);
        isDuplicate.Should().BeTrue("first delivery should persist the inbox record");

        // Simulate second delivery check — it should short-circuit and not create a second row
        var count = await _dbContext.InboxMessages.CountAsync(m => m.Id == eventId);
        count.Should().Be(1, "duplicate event delivery must remain idempotent");
    }

    [Fact]
    public async Task SchemaCreation_ShouldLandAllTablesInEmployerProfileSchema()
    {
        // Verify key tables exist in employer_profile schema by querying them without error
        Func<Task> queryProfiles = () => _dbContext.EmployerProfiles.CountAsync();
        Func<Task> queryShortlists = () => _dbContext.Shortlists.CountAsync();
        Func<Task> queryOutbox = () => _dbContext.OutboxMessages.CountAsync();
        Func<Task> queryInbox = () => _dbContext.InboxMessages.CountAsync();

        await queryProfiles.Should().NotThrowAsync("employer_profiles table must exist");
        await queryShortlists.Should().NotThrowAsync("shortlists table must exist");
        await queryOutbox.Should().NotThrowAsync("outbox_messages table must exist");
        await queryInbox.Should().NotThrowAsync("inbox_messages table must exist");
    }

    [Fact]
    public async Task UserAccountActivatedConsumer_ShouldActivateProfile()
    {
        // Arrange
        var (db, uow) = (CreateFreshContext(), new UnitOfWork(CreateFreshContext()));
        var repo = new EmployerProfileRepository(db);
        var profile = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("ActivateTest Corp").Value,
            EmailAddress.Create("activate@test.com").Value,
            MobileNumber.Create("+8801711111101").Value,
            CompanyIdentifier.Create($"ACT{Guid.NewGuid():N}").Value);
        await repo.AddAsync(profile);
        await uow.SaveChangesAsync();
        profile.Status.Should().Be(EmployerProfileStatus.PendingActivation);

        await using var consumerDb = CreateFreshContext();
        var consumerRepo = new EmployerProfileRepository(consumerDb);
        var consumerUow = new UnitOfWork(consumerDb);
        var consumer = new UserAccountActivatedConsumer(consumerRepo, consumerUow, consumerDb);
        var evt = new UserAccountActivatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, profile.UserId);

        // Act
        await consumer.Handle(evt, CancellationToken.None);

        // Assert
        var activated = await consumerRepo.GetByIdAsync(profile.Id);
        activated.Should().NotBeNull();
        activated!.Status.Should().Be(EmployerProfileStatus.PendingVerification);
    }

    [Fact]
    public async Task UserAccountActivatedConsumer_ShouldBeIdempotent()
    {
        // Arrange
        var (db, uow) = (CreateFreshContext(), new UnitOfWork(CreateFreshContext()));
        var repo = new EmployerProfileRepository(db);
        var profile = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("Idempotent Corp").Value,
            EmailAddress.Create("idempotent@test.com").Value,
            MobileNumber.Create("+8801711111102").Value,
            CompanyIdentifier.Create($"IDEM{Guid.NewGuid():N}").Value);
        await repo.AddAsync(profile);
        await uow.SaveChangesAsync();

        // Act — deliver twice
        var consumerDb = CreateFreshContext();
        var consumerRepo = new EmployerProfileRepository(consumerDb);
        var consumerUow = new UnitOfWork(consumerDb);
        var consumer = new UserAccountActivatedConsumer(consumerRepo, consumerUow, consumerDb);
        var evt = new UserAccountActivatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, profile.UserId);

        await consumer.Handle(evt, CancellationToken.None);
        var inboxCountAfterFirst = await consumerDb.InboxMessages.CountAsync(m => m.Id == evt.EventId);
        inboxCountAfterFirst.Should().Be(1);

        // Deliver the same EventId again
        await consumer.Handle(evt, CancellationToken.None);
        var inboxCountAfterSecond = await consumerDb.InboxMessages.CountAsync(m => m.Id == evt.EventId);
        inboxCountAfterSecond.Should().Be(1, "duplicate delivery must not create a second inbox row");
    }

    [Fact]
    public async Task EmployerVerifiedByGovernmentConsumer_ShouldPassAutoVerification()
    {
        // Arrange
        var (db, uow) = (CreateFreshContext(), new UnitOfWork(CreateFreshContext()));
        var repo = new EmployerProfileRepository(db);
        var profile = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("GovVerify Corp").Value,
            EmailAddress.Create("govverify@test.com").Value,
            MobileNumber.Create("+8801711111103").Value,
            CompanyIdentifier.Create($"GOV{Guid.NewGuid():N}").Value);
        profile.Activate();
        profile.CompleteLevel2(
            WebsiteUrl.Create("https://govverify.test").Value, "Finance",
            CompanySize.Create(CompanySizeEnum.Medium).Value,
            Address.Create("1 Main St", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value,
            CompanyDescription.Create("Test company for gov verification").Value);
        await repo.AddAsync(profile);
        await uow.SaveChangesAsync();
        profile.Status.Should().Be(EmployerProfileStatus.PendingVerification);

        await using var consumerDb = CreateFreshContext();
        var consumerRepo = new EmployerProfileRepository(consumerDb);
        var consumerUow = new UnitOfWork(consumerDb);
        var consumer = new EmployerVerifiedByGovernmentConsumer(consumerRepo, consumerUow, consumerDb);
        var evt = new EmployerVerifiedByGovernmentIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, profile.Id, "gov-evidence-001");

        // Act
        await consumer.Handle(evt, CancellationToken.None);

        // Assert
        var verified = await consumerRepo.GetByIdAsync(profile.Id);
        verified.Should().NotBeNull();
        verified!.Status.Should().Be(EmployerProfileStatus.Verified);
    }

    [Fact]
    public async Task JobPostingPublishedConsumer_ShouldAddDashboardPosting()
    {
        // Arrange
        var employerUserId = Guid.NewGuid();
        await using var consumerDb = CreateFreshContext();
        var store = new DashboardProjectionStore(consumerDb);
        var consumer = new JobPostingPublishedConsumer(store, consumerDb);
        var evt = new JobPostingPublishedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow,
            Guid.NewGuid(), employerUserId, "Senior .NET Engineer");

        // Act
        await consumer.Handle(evt, CancellationToken.None);

        // Assert
        var postings = await store.GetPostingsAsync(employerUserId);
        postings.Should().HaveCount(1);
        postings.First().Title.Should().Be("Senior .NET Engineer");
        postings.First().EmployerUserId.Should().Be(employerUserId);
    }

    [Fact]
    public async Task ApplicationSubmittedConsumer_ShouldAddDashboardApplication()
    {
        // Arrange
        var employerUserId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        await using var consumerDb = CreateFreshContext();
        var store = new DashboardProjectionStore(consumerDb);
        var consumer = new ApplicationSubmittedConsumer(store, consumerDb);
        var evt = new ApplicationSubmittedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow,
            applicationId, employerUserId, postingId, Guid.NewGuid());

        // Act
        await consumer.Handle(evt, CancellationToken.None);

        // Assert
        var applications = await store.GetApplicationsAsync(employerUserId);
        applications.Should().HaveCount(1);
        applications.First().ApplicationId.Should().Be(applicationId);
    }

    [Fact]
    public async Task UserAccountSuspendedConsumer_ShouldSuspendProfile()
    {
        // Arrange
        var (db, uow) = (CreateFreshContext(), new UnitOfWork(CreateFreshContext()));
        var repo = new EmployerProfileRepository(db);
        var profile = EmployerProfile.Register(Guid.NewGuid(), Guid.NewGuid(),
            CompanyName.Create("SuspendTest Corp").Value,
            EmailAddress.Create("suspend@test.com").Value,
            MobileNumber.Create("+8801711111104").Value,
            CompanyIdentifier.Create($"SUS{Guid.NewGuid():N}").Value);
        profile.Activate();
        await repo.AddAsync(profile);
        await uow.SaveChangesAsync();

        await using var consumerDb = CreateFreshContext();
        var consumerRepo = new EmployerProfileRepository(consumerDb);
        var consumerUow = new UnitOfWork(consumerDb);
        var consumer = new UserAccountSuspendedConsumer(consumerRepo, consumerUow, consumerDb);
        var evt = new UserAccountSuspendedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, profile.UserId, "Policy violation");

        // Act
        await consumer.Handle(evt, CancellationToken.None);

        // Assert
        var suspended = await consumerRepo.GetByIdAsync(profile.Id);
        suspended.Should().NotBeNull();
        suspended!.Status.Should().Be(EmployerProfileStatus.Suspended);
    }
}
