using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Interceptors;
using NSubstitute;

namespace Nexhire.Modules.JobPostings.Tests.Unit.Infrastructure;

public class OutboxTests
{
    [Fact]
    public async Task SaveChanges_ShouldPersistDomainEventsToOutbox_ForJobPostingsContext()
    {
        var options = new DbContextOptionsBuilder<JobPostingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var publisher = Substitute.For<IPublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(publisher);
        await using var dbContext = new JobPostingsDbContext(options, new PublishDomainEventsInterceptor(services.BuildServiceProvider()));
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

        dbContext.JobPostings.Add(posting);
        await dbContext.SaveChangesAsync();

        dbContext.OutboxMessages.Should().ContainSingle();
        await publisher.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }
}
