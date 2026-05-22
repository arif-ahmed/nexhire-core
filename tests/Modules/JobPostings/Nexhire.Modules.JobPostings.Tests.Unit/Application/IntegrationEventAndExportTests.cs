using FluentAssertions;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.IntegrationEvents;
using Nexhire.Modules.JobPostings.Infrastructure.Adapters;
using NSubstitute;

namespace Nexhire.Modules.JobPostings.Tests.Unit.Application;

public class IntegrationEventAndExportTests
{
    [Fact]
    public async Task EmployerVerified_ShouldUpsertEligibleStanding()
    {
        var standings = Substitute.For<IEmployerStandingStore>();
        var uow = Substitute.For<IJobPostingsUnitOfWork>();
        var handler = new EmployerStandingProjectionHandlers(standings, uow);
        var employerId = Guid.NewGuid();

        await handler.Handle(new EmployerVerifiedIntegrationEvent(employerId, DateTime.UtcNow), CancellationToken.None);

        await standings.Received(1).UpsertAsync(
            Arg.Is<EmployerStanding>(x => x.EmployerId == employerId && x.IsVerified && x.IsActive),
            Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AccountDeactivated_ShouldCloseOpenPostingsAndAuditSystemReason()
    {
        var standings = Substitute.For<IEmployerStandingStore>();
        var postings = Substitute.For<IJobPostingRepository>();
        var trails = Substitute.For<IPostingAuditTrailRepository>();
        var uow = Substitute.For<IJobPostingsUnitOfWork>();
        var posting = Draft();
        posting.Publish(new Core.Domain.Services.SchemaOrgStandardizer().Standardize(posting), new EmployerStanding(posting.EmployerId, true, true, DateTime.UtcNow));
        var trail = PostingAuditTrail.Create(posting.Id);
        postings.GetOpenByEmployerIdAsync(posting.EmployerId, Arg.Any<CancellationToken>()).Returns(new[] { posting });
        trails.GetByPostingIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(trail);
        var handler = new EmployerAccountClosedHandlers(standings, postings, trails, uow);

        await handler.Handle(new EmployerAccountDeactivatedIntegrationEvent(posting.EmployerId, DateTime.UtcNow), CancellationToken.None);

        posting.Status.Should().Be(PostingStatus.Archived);
        trail.Entries.Should().ContainSingle(e => e.Actor.Kind == AuditActorKind.System && e.Reason == "employer-account-deactivated");
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditExporter_ShouldSupportCsvAndPdf()
    {
        var trails = Substitute.For<IPostingAuditTrailRepository>();
        var postingId = Guid.NewGuid();
        var trail = PostingAuditTrail.Create(postingId);
        trail.RecordFieldEdit(AuditActor.System(), new[] { "Title" });
        trails.GetByPostingIdAsync(postingId, Arg.Any<CancellationToken>()).Returns(trail);
        var exporter = new CsvAuditTrailExporter(trails);

        var csv = await exporter.ExportAsync(postingId, "csv", CancellationToken.None);
        var pdf = await exporter.ExportAsync(postingId, "pdf", CancellationToken.None);

        csv.IsSuccess.Should().BeTrue();
        csv.Value.ContentType.Should().Be("text/csv");
        pdf.IsSuccess.Should().BeTrue();
        pdf.Value.ContentType.Should().Be("application/pdf");
        pdf.Value.Content.Take(4).Should().Equal("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task TaxonomyUpdated_ShouldFlagDeprecatedSkillCodesAndAudit()
    {
        var postings = Substitute.For<IJobPostingRepository>();
        var trails = Substitute.For<IPostingAuditTrailRepository>();
        var uow = Substitute.For<IJobPostingsUnitOfWork>();
        var posting = Draft();
        var trail = PostingAuditTrail.Create(posting.Id);
        postings.GetBySkillCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns(new[] { posting });
        trails.GetByPostingIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(trail);
        var handler = new TaxonomyUpdatedIntegrationEventHandler(postings, trails, uow);

        await handler.Handle(new TaxonomyUpdatedIntegrationEvent(new[] { "SK-CSHARP" }, DateTime.UtcNow), CancellationToken.None);

        posting.DeprecatedSkillCodes.Should().Contain("SK-CSHARP");
        trail.Entries.Should().ContainSingle(e => e.ChangedFields.Contains("DeprecatedSkill:SK-CSHARP"));
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static JobPosting Draft()
    {
        return JobPosting.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobTitle.Create("Backend Engineer").Value,
            JobSummary.Create("Build APIs and platform services for hiring workflows.").Value,
            ContractType.FullTime,
            EducationLevel.Bachelor,
            WorkFormat.Online,
            null,
            new[] { RequiredSkill.Create(CanonicalSkillRef.Create("SK-CSHARP", "C#").Value, "C#", SkillImportance.Mandatory).Value },
            Array.Empty<LanguageRequirement>(),
            ApplicationDeadline.Create(DateTime.UtcNow.AddDays(10), true).Value,
            null,
            null,
            PostingVisibility.Create(VisibilityLevel.Public).Value).Value;
    }
}
