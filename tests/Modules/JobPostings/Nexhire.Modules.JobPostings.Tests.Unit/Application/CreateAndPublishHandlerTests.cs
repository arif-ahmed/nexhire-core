using FluentAssertions;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.Services;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Modules.JobPostings.Core.JobPostings.Commands;
using Nexhire.Shared.Core.Results;
using NSubstitute;

namespace Nexhire.Modules.JobPostings.Tests.Unit.Application;

public class CreateAndPublishHandlerTests
{
    [Fact]
    public async Task Create_ShouldCanonicalizeSkillsAndCreateAuditTrail()
    {
        var postings = Substitute.For<IJobPostingRepository>();
        var audit = Substitute.For<IPostingAuditTrailRepository>();
        var taxonomy = Substitute.For<ITaxonomyApi>();
        var uow = Substitute.For<IJobPostingsUnitOfWork>();
        taxonomy.CanonicalizeSkillAsync("C#", Arg.Any<CancellationToken>())
            .Returns(CanonicalSkillRef.Create("SK-CSHARP", "C#"));

        var handler = new CreateJobPostingCommandHandler(postings, audit, taxonomy, uow);

        var result = await handler.Handle(new CreateJobPostingCommand(Guid.NewGuid(), Guid.NewGuid(), Draft()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await postings.Received(1).AddAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>());
        await audit.Received(1).AddAsync(Arg.Any<PostingAuditTrail>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ShouldFailForIneligibleEmployerWithoutSaving()
    {
        var posting = JobPosting.CreateDraft(
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

        var postings = Substitute.For<IJobPostingRepository>();
        var audit = Substitute.For<IPostingAuditTrailRepository>();
        var standing = Substitute.For<IEmployerStandingStore>();
        var taxonomy = Substitute.For<ITaxonomyApi>();
        var uow = Substitute.For<IJobPostingsUnitOfWork>();
        postings.GetByIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(posting);
        standing.GetAsync(posting.EmployerId, Arg.Any<CancellationToken>()).Returns(EmployerStanding.Ineligible(posting.EmployerId));
        taxonomy.IsValidSkillCodeAsync("SK-CSHARP", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new PublishJobPostingCommandHandler(postings, audit, standing, taxonomy, new SchemaOrgStandardizer(), uow);

        var result = await handler.Handle(new PublishJobPostingCommand(posting.Id, posting.EmployerId, posting.PostedByUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-POST-EMPLOYER-NOT-ELIGIBLE");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static JobPostingDraftDto Draft() =>
        new(
            "Backend Engineer",
            "Build APIs and platform services for hiring workflows.",
            ContractType.FullTime,
            EducationLevel.Bachelor,
            WorkFormat.Online,
            null,
            new[] { new SkillInput("C#") },
            new[] { new LanguageRequirementDto("English", "B2") },
            DateTime.UtcNow.AddDays(10),
            true,
            "https://example.com/jobs/backend",
            new SalaryRangeDto(1000, 2000, "BDT", SalaryPeriod.Monthly),
            new PostingVisibilityDto(VisibilityLevel.Public, null));
}
