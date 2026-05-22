using FluentAssertions;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Services;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobPostings.Tests.Unit.Domain;

public class JobPostingDomainTests
{
    [Fact]
    public void CreateDraft_ShouldRejectPhysicalPostingWithoutLocation()
    {
        var result = JobPosting.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobTitle.Create("Backend Engineer").Value,
            JobSummary.Create("Build APIs and platform services for hiring workflows.").Value,
            ContractType.FullTime,
            EducationLevel.Bachelor,
            WorkFormat.Physical,
            null,
            new[] { Skill("C#") },
            new[] { LanguageRequirement.Create("English", "B2").Value },
            ApplicationDeadline.Create(DateTime.UtcNow.AddDays(10), true).Value,
            null,
            null,
            PostingVisibility.Create(VisibilityLevel.Public).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-POST-LOCATION-REQUIRED");
    }

    [Fact]
    public void Publish_ShouldRequireEligibleEmployerAndSchemaCompliance()
    {
        var posting = Draft();
        var schema = new SchemaOrgStandardizer().Standardize(posting);

        var result = posting.Publish(schema, EmployerStanding.Ineligible(posting.EmployerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-POST-EMPLOYER-NOT-ELIGIBLE");
    }

    [Fact]
    public void StatusMachine_ShouldAllowPublishPauseResumeExpireArchive()
    {
        var posting = Draft();
        var standing = new EmployerStanding(posting.EmployerId, true, true, DateTime.UtcNow);
        var publish = posting.Publish(new SchemaOrgStandardizer().Standardize(posting), standing);
        var pause = posting.Pause();
        var resume = posting.Resume();
        var expire = posting.Expire();
        var archive = posting.Archive();

        publish.IsSuccess.Should().BeTrue();
        pause.IsSuccess.Should().BeTrue();
        resume.IsSuccess.Should().BeTrue();
        expire.IsSuccess.Should().BeTrue();
        archive.IsSuccess.Should().BeTrue();
        posting.Status.Should().Be(PostingStatus.Archived);
    }

    [Theory]
    [InlineData(PostingStatus.Draft, PostingStatus.Paused)]
    [InlineData(PostingStatus.Expired, PostingStatus.Active)]
    [InlineData(PostingStatus.Archived, PostingStatus.Active)]
    [InlineData(PostingStatus.Removed, PostingStatus.Active)]
    [InlineData(PostingStatus.Draft, PostingStatus.Archived)]
    public void StatusTransition_ShouldRejectRepresentativeIllegalPairs(PostingStatus from, PostingStatus to)
    {
        StatusTransition.Create(from, to).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ExtendDeadline_ShouldRejectEarlierOrEqualDeadline()
    {
        var posting = Draft();

        var result = posting.ExtendDeadline(ApplicationDeadline.Create(DateTime.UtcNow.AddDays(5), true).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-POST-DEADLINE-NOT-LATER");
    }

    [Fact]
    public void RenewFrom_ShouldCreateNewDraftWithoutMutatingSource()
    {
        var posting = Draft();
        posting.Publish(new SchemaOrgStandardizer().Standardize(posting), new EmployerStanding(posting.EmployerId, true, true, DateTime.UtcNow));
        posting.Expire();

        var renewed = JobPosting.RenewFrom(posting, ApplicationDeadline.Create(DateTime.UtcNow.AddDays(30), true).Value);

        renewed.IsSuccess.Should().BeTrue();
        renewed.Value.Id.Should().NotBe(posting.Id);
        renewed.Value.RenewedFromPostingId.Should().Be(posting.Id);
        renewed.Value.Status.Should().Be(PostingStatus.Draft);
        posting.Status.Should().Be(PostingStatus.Expired);
    }

    [Fact]
    public void PostingAuditTrail_ShouldAppendStatusAndFieldEntries()
    {
        var trail = PostingAuditTrail.Create(Guid.NewGuid());
        var actor = AuditActor.Create(AuditActorKind.Admin, Guid.NewGuid(), "Admin").Value;

        trail.RecordStatusChange(actor, StatusTransition.Create(PostingStatus.Active, PostingStatus.Suspended).Value, "Policy breach");
        trail.RecordFieldEdit(actor, new[] { "Title", "Summary" });

        trail.Entries.Should().HaveCount(2);
        trail.Entries.Select(e => e.Kind).Should().ContainInOrder(AuditEntryKind.StatusChange, AuditEntryKind.FieldEdit);
    }

    [Fact]
    public void ExpirationPolicy_ShouldExpireOnlyActiveOrPausedPastDeadline()
    {
        var posting = Draft();
        posting.Publish(new SchemaOrgStandardizer().Standardize(posting), new EmployerStanding(posting.EmployerId, true, true, DateTime.UtcNow));
        typeof(JobPosting).GetProperty(nameof(JobPosting.Deadline))!.SetValue(posting, ApplicationDeadline.Rehydrate(DateTime.UtcNow.AddMinutes(-1), true));

        new PostingExpirationPolicy().ShouldExpire(posting, DateTime.UtcNow).Should().BeTrue();
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
            new[] { Skill("C#") },
            new[] { LanguageRequirement.Create("English", "B2").Value },
            ApplicationDeadline.Create(DateTime.UtcNow.AddDays(10), true).Value,
            JobPostingLink.Create("https://example.com/jobs/backend").Value,
            SalaryRange.Create(1000, 2000).Value,
            PostingVisibility.Create(VisibilityLevel.Public).Value).Value;
    }

    private static RequiredSkill Skill(string label)
    {
        var canonical = CanonicalSkillRef.Create($"SK-{label.ToUpperInvariant()}", label).Value;
        return RequiredSkill.Create(canonical, label, SkillImportance.Mandatory).Value;
    }
}
