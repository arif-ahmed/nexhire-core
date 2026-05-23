using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MediatR;
using NSubstitute;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence;
using Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Xunit;

namespace Nexhire.Modules.AdministratorsConfiguration.Tests.Integration;

public class TaxonomyPersistenceTests
{
    private readonly PublishDomainEventsInterceptor _interceptor;

    public TaxonomyPersistenceTests()
    {
        var publisher = Substitute.For<IPublisher>();
        _interceptor = new PublishDomainEventsInterceptor(publisher);
    }

    [Fact]
    public async Task EFCore_Should_SuccessfullyRoundTripTaxonomyAndTerms()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdministratorsConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AdministratorsConfigurationDbContext(options, _interceptor);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new TaxonomyRepository(dbContext);

        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills Taxonomy");
        var code1 = TermCode.Create("SKILL.PYTHON").Value;
        var code2 = TermCode.Create("SKILL.CSHARP").Value;

        taxonomy.AddTerm(code1, "Python", SkillCategory.Hard, null);
        taxonomy.AddTerm(code2, "C#", SkillCategory.Hard, null);

        // Act
        await repository.AddAsync(taxonomy, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Load back from a fresh context to ensure fully persistent round-trip
        await using var freshContext = new AdministratorsConfigurationDbContext(options, _interceptor);
        var freshRepository = new TaxonomyRepository(freshContext);
        
        var loadedTaxonomy = await freshRepository.GetByKindAsync(TaxonomyKind.Skills, CancellationToken.None);

        // Assert
        loadedTaxonomy.Should().NotBeNull();
        loadedTaxonomy!.Name.Should().Be("Skills Taxonomy");
        loadedTaxonomy.Terms.Should().HaveCount(2);

        var loadedPython = loadedTaxonomy.Terms.First(t => t.Code == code1);
        loadedPython.Label.Should().Be("Python");
        loadedPython.Category.Should().Be(SkillCategory.Hard);
        loadedPython.Status.Should().Be(TermStatus.Active);
    }

    [Fact]
    public async Task EFCore_Should_ThrowDbUpdateConcurrencyException_WhenOptimisticConcurrencyFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdministratorsConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Seed taxonomy with an existing term
        await using (var dbContext = new AdministratorsConfigurationDbContext(options, _interceptor))
        {
            await dbContext.Database.EnsureCreatedAsync();
            var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
            taxonomy.AddTerm(TermCode.Create("SKILL.EXISTING").Value, "Existing", SkillCategory.Hard, null);
            dbContext.Taxonomies.Add(taxonomy);
            await dbContext.SaveChangesAsync();
        }

        // Simulating two parallel admin threads fetching same state:
        await using var thread1Context = new AdministratorsConfigurationDbContext(options, _interceptor);
        await using var thread2Context = new AdministratorsConfigurationDbContext(options, _interceptor);

        var tax1 = await thread1Context.Taxonomies.Include(t => t.Terms).FirstAsync(t => t.Kind == TaxonomyKind.Skills);
        var tax2 = await thread2Context.Taxonomies.Include(t => t.Terms).FirstAsync(t => t.Kind == TaxonomyKind.Skills);

        var termCode = TermCode.Create("SKILL.EXISTING").Value;

        // Thread 1 edits and saves first:
        tax1.RenameTerm(termCode, "Python");
        await thread1Context.SaveChangesAsync();

        // Thread 2 edits and attempts to save stale state:
        tax2.RenameTerm(termCode, "C#");
        
        // Act & Assert
        Func<Task> saveStaleAction = async () => await thread2Context.SaveChangesAsync();
        await saveStaleAction.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
