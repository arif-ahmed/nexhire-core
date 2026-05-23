using FluentAssertions;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Commands;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.DTOs;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Ports;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Queries;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Nexhire.Modules.AdministratorsConfiguration.Tests.Unit;

public class TaxonomyApplicationTests
{
    private readonly ITaxonomyRepository _repositoryMock;
    private readonly IAdministratorsConfigurationUnitOfWork _unitOfWorkMock;

    public TaxonomyApplicationTests()
    {
        _repositoryMock = Substitute.For<ITaxonomyRepository>();
        _unitOfWorkMock = Substitute.For<IAdministratorsConfigurationUnitOfWork>();
    }

    [Fact]
    public async Task AddTaxonomyTermHandler_Should_StagedSuccessfully_When_Valid()
    {
        // Arrange
        var handler = new AddTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>())
                       .Returns(taxonomy);

        var command = new AddTaxonomyTermCommand("Skills", "SKILL.CSHARP", "C#", "Hard", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.Should().ContainSingle(t => t.Code.Value == "SKILL.CSHARP" && t.Label == "C#");
        
        _repositoryMock.Received(1).Update(taxonomy);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddTaxonomyTermHandler_Should_ReturnFailure_When_TaxonomyNotFound()
    {
        // Arrange
        var handler = new AddTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>())
                       .Returns((Taxonomy)null!);

        var command = new AddTaxonomyTermCommand("Skills", "SKILL.CSHARP", "C#", "Hard", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
        await _unitOfWorkMock.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task BulkImportTaxonomyHandler_Should_StagedSuccessfully_And_ReturnReport()
    {
        // Arrange
        var csvReaderMock = Substitute.For<ICsvReader>();
        var importService = new TaxonomyImportService();
        var handler = new BulkImportTaxonomyCommandHandler(_repositoryMock, csvReaderMock, importService, _unitOfWorkMock);
        
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>())
                       .Returns(taxonomy);

        var rawRows = new List<RawImportRow>
        {
            new(1, "SKILL.A", "Skill A", "Hard", null),
            new(2, "SKILL.A", "Skill A Dup", "Hard", null), // Duplicate row
            new(3, "SKILL.B", "Skill B", "Soft", null)
        };

        var stream = new MemoryStream();
        csvReaderMock.Read(stream).Returns(rawRows);

        var command = new BulkImportTaxonomyCommand("Skills", stream);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var importReport = result.Value;
        importReport.SucceededCount.Should().Be(2);
        importReport.FailedCount.Should().Be(1);
        importReport.Rows.Should().HaveCount(3);

        importReport.Rows[0].Succeeded.Should().BeTrue();
        importReport.Rows[1].Succeeded.Should().BeFalse(); // Dup
        importReport.Rows[1].ErrorCode.Should().Be("E-TAXO-DUPLICATE-CODE");
        importReport.Rows[2].Succeeded.Should().BeTrue();

        taxonomy.Terms.Should().HaveCount(2);
        _repositoryMock.Received(1).Update(taxonomy);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTaxonomyQueryHandler_Should_ReturnHierarchicalTree()
    {
        // Arrange
        var handler = new GetTaxonomyQueryHandler(_repositoryMock);
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations Taxonomy");
        
        var p = TermCode.Create("OCC.TECH").Value;
        var c = TermCode.Create("OCC.DEV").Value;
        
        taxonomy.AddTerm(p, "Tech Sector", null, null);
        taxonomy.AddTerm(c, "Developer", null, p);

        _repositoryMock.GetByKindAsync(TaxonomyKind.Occupations, Arg.Any<CancellationToken>())
                       .Returns(taxonomy);

        var query = new GetTaxonomyQuery("Occupations");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Name.Should().Be("Occupations Taxonomy");
        dto.Kind.Should().Be("Occupations");
        
        dto.Terms.Should().ContainSingle(); // Roots only at the top level
        var rootNode = dto.Terms.First();
        rootNode.Code.Should().Be("OCC.TECH");
        rootNode.Children.Should().ContainSingle();
        rootNode.Children.First().Code.Should().Be("OCC.DEV");
    }

    [Fact]
    public async Task SeedTaxonomiesHandler_Should_CreateThreeTaxonomies_WhenNoneExist()
    {
        _repositoryMock.GetByKindAsync(Arg.Any<TaxonomyKind>(), Arg.Any<CancellationToken>())
                       .Returns((Taxonomy)null!);

        var handler = new SeedTaxonomiesCommandHandler(_repositoryMock, _unitOfWorkMock);

        var result = await handler.Handle(new SeedTaxonomiesCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repositoryMock.Received(3).AddAsync(Arg.Any<Taxonomy>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedTaxonomiesHandler_Should_BeIdempotent_WhenAlreadySeeded()
    {
        var skillsTaxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var occTaxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var trainTaxonomy = Taxonomy.Create(TaxonomyKind.TrainingPrograms, "Training Programs");

        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns(skillsTaxonomy);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Occupations, Arg.Any<CancellationToken>()).Returns(occTaxonomy);
        _repositoryMock.GetByKindAsync(TaxonomyKind.TrainingPrograms, Arg.Any<CancellationToken>()).Returns(trainTaxonomy);

        var handler = new SeedTaxonomiesCommandHandler(_repositoryMock, _unitOfWorkMock);

        var result = await handler.Handle(new SeedTaxonomiesCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repositoryMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task RenameTaxonomyTermHandler_Should_Succeed_WhenValid()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        taxonomy.AddTerm(TermCode.Create("SKILL.PYTHON").Value, "Python", SkillCategory.Hard, null);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns(taxonomy);

        var handler = new RenameTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new RenameTaxonomyTermCommand("Skills", "SKILL.PYTHON", "Python 3"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().Label.Should().Be("Python 3");
        _repositoryMock.Received(1).Update(taxonomy);
    }

    [Fact]
    public async Task RenameTaxonomyTermHandler_Should_Fail_WhenTaxonomyNotFound()
    {
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var handler = new RenameTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new RenameTaxonomyTermCommand("Skills", "SKILL.PYTHON", "New"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task RecategorizeSkillHandler_Should_Succeed_WhenValid()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        taxonomy.AddTerm(TermCode.Create("SKILL.PYTHON").Value, "Python", SkillCategory.Hard, null);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns(taxonomy);

        var handler = new RecategorizeSkillCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new RecategorizeSkillCommand("SKILL.PYTHON", "Soft"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().Category.Should().Be(SkillCategory.Soft);
    }

    [Fact]
    public async Task RecategorizeSkillHandler_Should_Fail_WhenSkillsTaxonomyNotFound()
    {
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var handler = new RecategorizeSkillCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new RecategorizeSkillCommand("SKILL.PYTHON", "Soft"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task ReparentTaxonomyTermHandler_Should_Succeed_WhenValid()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var parent = TermCode.Create("OCC.TECH").Value;
        var child = TermCode.Create("OCC.DEV").Value;
        taxonomy.AddTerm(parent, "Tech", null, null);
        taxonomy.AddTerm(child, "Developer", null, null);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Occupations, Arg.Any<CancellationToken>()).Returns(taxonomy);

        var handler = new ReparentTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new ReparentTaxonomyTermCommand("Occupations", "OCC.DEV", "OCC.TECH"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First(t => t.Code == child).ParentCode.Should().Be(parent);
    }

    [Fact]
    public async Task ReparentTaxonomyTermHandler_Should_Fail_WhenTaxonomyNotFound()
    {
        _repositoryMock.GetByKindAsync(TaxonomyKind.Occupations, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var handler = new ReparentTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(new ReparentTaxonomyTermCommand("Occupations", "OCC.DEV", "OCC.TECH"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task DeprecateTaxonomyTermHandler_Should_Succeed_WhenValid()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        taxonomy.AddTerm(TermCode.Create("SKILL.PYTHON2").Value, "Python 2", SkillCategory.Hard, null);
        taxonomy.AddTerm(TermCode.Create("SKILL.PYTHON3").Value, "Python 3", SkillCategory.Hard, null);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns(taxonomy);

        var handler = new DeprecateTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(
            new DeprecateTaxonomyTermCommand("Skills", "SKILL.PYTHON2", "SKILL.PYTHON3"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First(t => t.Code.Value == "SKILL.PYTHON2").Status.Should().Be(TermStatus.Deprecated);
    }

    [Fact]
    public async Task DeprecateTaxonomyTermHandler_Should_Fail_WhenTaxonomyNotFound()
    {
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var handler = new DeprecateTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(
            new DeprecateTaxonomyTermCommand("Skills", "SKILL.PYTHON", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task ReactivateTaxonomyTermHandler_Should_Succeed_WhenValid()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        taxonomy.AddTerm(TermCode.Create("SKILL.PYTHON").Value, "Python", SkillCategory.Hard, null);
        taxonomy.DeprecateTerm(TermCode.Create("SKILL.PYTHON").Value, null);
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns(taxonomy);

        var handler = new ReactivateTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(
            new ReactivateTaxonomyTermCommand("Skills", "SKILL.PYTHON"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First(t => t.Code.Value == "SKILL.PYTHON").Status.Should().Be(TermStatus.Active);
    }

    [Fact]
    public async Task ReactivateTaxonomyTermHandler_Should_Fail_WhenTaxonomyNotFound()
    {
        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var handler = new ReactivateTaxonomyTermCommandHandler(_repositoryMock, _unitOfWorkMock);
        var result = await handler.Handle(
            new ReactivateTaxonomyTermCommand("Skills", "SKILL.PYTHON"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task BulkImportHandler_Should_Fail_WhenTaxonomyNotFound()
    {
        var csvReaderMock = Substitute.For<ICsvReader>();
        csvReaderMock.Read(Arg.Any<Stream>()).Returns(new List<RawImportRow>());
        var importService = new TaxonomyImportService();
        var handler = new BulkImportTaxonomyCommandHandler(_repositoryMock, csvReaderMock, importService, _unitOfWorkMock);

        _repositoryMock.GetByKindAsync(TaxonomyKind.Skills, Arg.Any<CancellationToken>()).Returns((Taxonomy)null!);

        var stream = new MemoryStream();
        var result = await handler.Handle(new BulkImportTaxonomyCommand("Skills", stream), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-NOT-FOUND");
    }

    [Fact]
    public async Task BulkImportHandler_Should_Fail_WhenKindInvalid()
    {
        var csvReaderMock = Substitute.For<ICsvReader>();
        var importService = new TaxonomyImportService();
        var handler = new BulkImportTaxonomyCommandHandler(_repositoryMock, csvReaderMock, importService, _unitOfWorkMock);

        var stream = new MemoryStream();
        var result = await handler.Handle(new BulkImportTaxonomyCommand("InvalidKind", stream), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-INVALID-KIND");
    }
}
