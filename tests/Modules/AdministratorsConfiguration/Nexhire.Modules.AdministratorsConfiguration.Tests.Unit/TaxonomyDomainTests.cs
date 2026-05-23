using FluentAssertions;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.AdministratorsConfiguration.Tests.Unit;

public class TaxonomyDomainTests
{
    [Theory]
    [InlineData("SKILL.PYTHON", true)]
    [InlineData("SKILL.DOTNET_CORE", true)]
    [InlineData("SKILL.AWS-LAMBDA-2023", true)]
    [InlineData("OCC.SOFTWARE-ENGINEER", true)]
    [InlineData("TRAIN.BOOTCAMP", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("SKILL.PYTHON!", false)] // Invalid char !
    [InlineData("SKILLPYTHON", false)]  // Missing separator dot
    [InlineData("SKILL.TOO_LONG_123456789012345678901234567890123456789012345678901234567890", false)] // > 64 chars
    public void TermCode_Validation_Should_Match_Invariants(string input, bool expectedSuccess)
    {
        // Act
        var result = TermCode.Create(input);

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Value.Value.Should().Be(input.ToUpperInvariant().Trim());
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void Taxonomy_Create_Should_Initialize_Version_And_Raise_Created_Event()
    {
        // Act
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills Taxonomy");

        // Assert
        taxonomy.Id.Should().NotBeEmpty();
        taxonomy.Kind.Should().Be(TaxonomyKind.Skills);
        taxonomy.Name.Should().Be("Skills Taxonomy");
        taxonomy.Version.Should().Be(1);
        taxonomy.Terms.Should().BeEmpty();
        
        taxonomy.DomainEvents.Should().ContainSingle(e => e is TaxonomyCreatedDomainEvent);
    }

    [Fact]
    public void AddTerm_HappyPath_Should_StageTerm_BumpVersion_And_RaiseEvents()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;

        // Act
        var result = taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.Should().ContainSingle();
        var term = taxonomy.Terms.First();
        term.Code.Should().Be(code);
        term.Label.Should().Be("Python");
        term.Category.Should().Be(SkillCategory.Hard);
        term.ParentCode.Should().BeNull();
        term.Status.Should().Be(TermStatus.Active);
        taxonomy.Version.Should().Be(2);

        taxonomy.DomainEvents.Should().HaveCount(3); // Created + Added + Updated
        taxonomy.DomainEvents.Should().Contain(e => e is TaxonomyTermAddedDomainEvent);
        taxonomy.DomainEvents.Should().Contain(e => e is TaxonomyUpdatedDomainEvent);
    }

    [Fact]
    public void AddTerm_DuplicateCode_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.AddTerm(code, "Python Duplicate", SkillCategory.Soft, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-DUPLICATE-CODE");
        taxonomy.Terms.Should().HaveCount(1);
        taxonomy.Version.Should().Be(2); // Still 2, not bumped
        taxonomy.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddTerm_PrefixMismatch_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var badCode = TermCode.Create("OCC.SOFTWARE-ENGINEER").Value; // Wrong prefix

        // Act
        var result = taxonomy.AddTerm(badCode, "Software Engineer", null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-CODE-PREFIX-MISMATCH");
    }

    [Fact]
    public void AddTerm_Skills_RequiresCategory_Should_Fail_When_Null()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;

        // Act
        var result = taxonomy.AddTerm(code, "Python", null, null); // Category is null

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-CATEGORY-REQUIRED");
    }

    [Fact]
    public void AddTerm_NonSkills_RejectsCategory_Should_Fail_When_NotNull()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var code = TermCode.Create("OCC.SOFTWARE-ENGINEER").Value;

        // Act
        var result = taxonomy.AddTerm(code, "Software Engineer", SkillCategory.Hard, null); // Category provided

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-INVALID-CATEGORY");
    }

    [Fact]
    public void AddTerm_ParentNotFound_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        var parentCode = TermCode.Create("SKILL.LANGUAGES").Value; // Parent doesn't exist

        // Act
        var result = taxonomy.AddTerm(code, "Python", SkillCategory.Hard, parentCode);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-PARENT-NOT-FOUND");
    }

    [Fact]
    public void AddTerm_ParentDeprecated_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var parentCode = TermCode.Create("SKILL.LANGUAGES").Value;
        var childCode = TermCode.Create("SKILL.PYTHON").Value;
        
        taxonomy.AddTerm(parentCode, "Languages", SkillCategory.Hard, null);
        taxonomy.DeprecateTerm(parentCode, null); // Parent is deprecated
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.AddTerm(childCode, "Python", SkillCategory.Hard, parentCode);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-PARENT-DEPRECATED");
    }

    [Fact]
    public void RenameTerm_HappyPath_Should_UpdateLabel()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);

        // Act
        var result = taxonomy.RenameTerm(code, "Python 3");

        // Assert
        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().Label.Should().Be("Python 3");
        taxonomy.Version.Should().Be(3);
    }

    [Fact]
    public void RecategorizeTerm_HappyPath_Should_UpdateCategory()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);

        // Act
        var result = taxonomy.RecategorizeTerm(code, SkillCategory.Soft);

        // Assert
        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().Category.Should().Be(SkillCategory.Soft);
        taxonomy.Version.Should().Be(3);
    }

    [Fact]
    public void ReparentTerm_CyclicDependency_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var p = TermCode.Create("OCC.TECH").Value;
        var c1 = TermCode.Create("OCC.DEV").Value;
        var c2 = TermCode.Create("OCC.WEBDEV").Value;

        taxonomy.AddTerm(p, "Tech Sector", null, null);
        taxonomy.AddTerm(c1, "Developer", null, p);
        taxonomy.AddTerm(c2, "Web Developer", null, c1);
        
        // At this stage: Tech Sector -> Developer -> Web Developer
        // We try to reparent "Tech Sector" (p) to "Web Developer" (c2) - Cycle!

        // Act
        var result = taxonomy.ReparentTerm(p, c2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-CYCLE");
    }

    [Fact]
    public void DeprecateTerm_Should_SoftDelete_PreservingTerm()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON2").Value;
        var replacement = TermCode.Create("SKILL.PYTHON3").Value;

        taxonomy.AddTerm(code, "Python 2", SkillCategory.Hard, null);
        taxonomy.AddTerm(replacement, "Python 3", SkillCategory.Hard, null);
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.DeprecateTerm(code, replacement);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var term = taxonomy.Terms.First(t => t.Code == code);
        term.Status.Should().Be(TermStatus.Deprecated);
        term.ReplacedByCode.Should().Be(replacement);
        term.DeprecatedOnUtc.Should().NotBeNull();
        
        taxonomy.Terms.Should().HaveCount(2); // Keeps both, no physical delete
        taxonomy.Version.Should().Be(4);
        
        taxonomy.DomainEvents.Should().ContainSingle(e => e is TaxonomyTermDeprecatedDomainEvent);
    }

    [Fact]
    public void DeprecateTerm_SelfReplace_Should_Fail()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);

        // Act
        var result = taxonomy.DeprecateTerm(code, code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-SELF-REPLACE");
    }

    [Fact]
    public void ApplyUsageDelta_Should_ClampAtZero_And_NotBumpVersion()
    {
        // Arrange
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);
        var baseVersion = taxonomy.Version;

        // Act & Assert 1: Add +1
        taxonomy.ApplyUsageDelta(code, 1).IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().UsageCount.Should().Be(1);
        taxonomy.Version.Should().Be(baseVersion); // Version unchanged

        // Act & Assert 2: Subtract -2 (should clamp at 0)
        taxonomy.ApplyUsageDelta(code, -2).IsSuccess.Should().BeTrue();
        taxonomy.Terms.First().UsageCount.Should().Be(0);
        taxonomy.Version.Should().Be(baseVersion); // Version unchanged
    }

    [Fact]
    public void TaxonomyImportService_TopologicalStaging_Should_StagedBatchWithDependenciesSuccessfully()
    {
        // Arrange
        var service = new TaxonomyImportService();
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");

        // Input rows where parent is defined after the child.
        // Topological sort must stage parents first.
        var rows = new List<RawImportRow>
        {
            new(1, "OCC.DEV", "Developer", null, "OCC.TECH"), // Child depends on parent in batch
            new(2, "OCC.TECH", "Tech Sector", null, null),    // Parent
            new(3, "OCC.WEBDEV", "Web Dev", null, "OCC.DEV")  // Grandchild depends on child in batch
        };

        // Act
        var result = service.ValidateAndStage(taxonomy, rows);

        // Assert
        result.SucceededCount.Should().Be(3);
        result.FailedCount.Should().Be(0);
        taxonomy.Terms.Should().HaveCount(3);
        
        // Assert they staged correctly with correct parent linkages
        var tech = taxonomy.Terms.First(t => t.Code.Value == "OCC.TECH");
        var dev = taxonomy.Terms.First(t => t.Code.Value == "OCC.DEV");
        var webdev = taxonomy.Terms.First(t => t.Code.Value == "OCC.WEBDEV");

        tech.ParentCode.Should().BeNull();
        dev.ParentCode.Value.Should().Be("OCC.TECH");
        webdev.ParentCode.Value.Should().Be("OCC.DEV");
    }

    [Fact]
    public void TaxonomyImportService_Staging_Should_GracefullyReportCyclicBatchRowAsFailure()
    {
        // Arrange
        var service = new TaxonomyImportService();
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");

        var rows = new List<RawImportRow>
        {
            new(1, "OCC.A", "A", null, "OCC.B"), // A's parent is B
            new(2, "OCC.B", "B", null, "OCC.A")  // B's parent is A (Cycle!)
        };

        // Act
        var result = service.ValidateAndStage(taxonomy, rows);

        // Assert
        result.SucceededCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        
        result.Rows.Should().HaveCount(2);
        result.Rows[0].ErrorCode.Should().Be("E-TAXO-CYCLE-IN-BATCH");
        result.Rows[1].ErrorCode.Should().Be("E-TAXO-CYCLE-IN-BATCH");
        taxonomy.Terms.Should().BeEmpty();
    }

    [Fact]
    public void BulkImportViaService_Should_BumpVersionExactlyOnce_RegardlessOfRowCount()
    {
        // Arrange
        var service = new TaxonomyImportService();
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        taxonomy.ClearDomainEvents();

        var rows = new List<RawImportRow>
        {
            new(1, "SKILL.PYTHON", "Python", "Hard", null),
            new(2, "SKILL.JAVA", "Java", "Hard", null),
            new(3, "SKILL.CSS", "CSS", "Soft", null)
        };

        // Act
        var result = service.ValidateAndStage(taxonomy, rows);

        // Assert - all 3 terms staged successfully
        result.SucceededCount.Should().Be(3);
        taxonomy.Terms.Should().HaveCount(3);

        // Before FinalizeImport, version is still 1 (no bumps from TryAddTermForImport)
        taxonomy.Version.Should().Be(1);

        // FinalizeImport bumps version exactly once
        taxonomy.FinalizeImport();
        taxonomy.Version.Should().Be(2);

        // Should have TaxonomyUpdated event from FinalizeImport + 3 TaxonomyTermAdded events
        taxonomy.DomainEvents.Should().ContainSingle(e => e is TaxonomyUpdatedDomainEvent);
        taxonomy.DomainEvents.OfType<TaxonomyTermAddedDomainEvent>().Should().HaveCount(3);
    }

    [Fact]
    public void RenameTerm_Should_Fail_When_TermNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;

        var result = taxonomy.RenameTerm(code, "New Name");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-TERM-NOT-FOUND");
    }

    [Fact]
    public void RecategorizeTerm_Should_Fail_When_NotSkillsTaxonomy()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var code = TermCode.Create("OCC.DEV").Value;
        taxonomy.AddTerm(code, "Developer", null, null);

        var result = taxonomy.RecategorizeTerm(code, SkillCategory.Hard);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-INVALID-OPERATION");
    }

    [Fact]
    public void RecategorizeTerm_Should_Fail_When_TermNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;

        var result = taxonomy.RecategorizeTerm(code, SkillCategory.Soft);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-TERM-NOT-FOUND");
    }

    [Fact]
    public void ReparentTerm_Should_Fail_When_TermNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var code = TermCode.Create("OCC.DEV").Value;

        var result = taxonomy.ReparentTerm(code, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-TERM-NOT-FOUND");
    }

    [Fact]
    public void ReparentTerm_Should_Fail_When_ParentNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var code = TermCode.Create("OCC.DEV").Value;
        taxonomy.AddTerm(code, "Developer", null, null);
        var badParent = TermCode.Create("OCC.NONEXISTENT").Value;

        var result = taxonomy.ReparentTerm(code, badParent);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-PARENT-NOT-FOUND");
    }

    [Fact]
    public void ReparentTerm_Should_Fail_When_ParentDeprecated()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var parentCode = TermCode.Create("OCC.TECH").Value;
        var childCode = TermCode.Create("OCC.DEV").Value;
        taxonomy.AddTerm(parentCode, "Tech", null, null);
        taxonomy.AddTerm(childCode, "Developer", null, parentCode);
        taxonomy.DeprecateTerm(parentCode, null);
        taxonomy.ClearDomainEvents();

        var result = taxonomy.ReparentTerm(childCode, parentCode);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-PARENT-DEPRECATED");
    }

    [Fact]
    public void ReparentTerm_Should_Succeed_When_RemovingParent()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        var parent = TermCode.Create("OCC.TECH").Value;
        var child = TermCode.Create("OCC.DEV").Value;
        taxonomy.AddTerm(parent, "Tech", null, null);
        taxonomy.AddTerm(child, "Developer", null, parent);

        var result = taxonomy.ReparentTerm(child, null);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Terms.First(t => t.Code == child).ParentCode.Should().BeNull();
    }

    [Fact]
    public void DeprecateTerm_Should_BeIdempotent_When_AlreadyDeprecated()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);
        taxonomy.DeprecateTerm(code, null);
        taxonomy.ClearDomainEvents();
        var versionBefore = taxonomy.Version;

        var result = taxonomy.DeprecateTerm(code, null);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Version.Should().Be(versionBefore);
        taxonomy.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DeprecateTerm_Should_Fail_When_ReplacementNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);
        var replacement = TermCode.Create("SKILL.NONEXISTENT").Value;

        var result = taxonomy.DeprecateTerm(code, replacement);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-REPLACEMENT-NOT-FOUND");
    }

    [Fact]
    public void ReactivateTerm_Should_BeIdempotent_When_AlreadyActive()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(code, "Python", SkillCategory.Hard, null);
        taxonomy.ClearDomainEvents();
        var versionBefore = taxonomy.Version;

        var result = taxonomy.ReactivateTerm(code);

        result.IsSuccess.Should().BeTrue();
        taxonomy.Version.Should().Be(versionBefore);
        taxonomy.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReactivateTerm_Should_Fail_When_ParentDeprecated()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var parentCode = TermCode.Create("SKILL.LANGUAGES").Value;
        var childCode = TermCode.Create("SKILL.PYTHON").Value;
        taxonomy.AddTerm(parentCode, "Languages", SkillCategory.Hard, null);
        taxonomy.AddTerm(childCode, "Python", SkillCategory.Hard, parentCode);
        taxonomy.DeprecateTerm(childCode, null);
        taxonomy.DeprecateTerm(parentCode, null);
        taxonomy.ClearDomainEvents();

        var result = taxonomy.ReactivateTerm(childCode);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-PARENT-DEPRECATED");
    }

    [Fact]
    public void ApplyUsageDelta_Should_Fail_When_TermNotFound()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Skills, "Skills");
        var code = TermCode.Create("SKILL.PYTHON").Value;

        var result = taxonomy.ApplyUsageDelta(code, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TAXO-TERM-NOT-FOUND");
    }

    [Fact]
    public void TaxonomyCreate_Should_SetKind_ForOccupations()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.Occupations, "Occupations");
        taxonomy.Kind.Should().Be(TaxonomyKind.Occupations);
        taxonomy.Name.Should().Be("Occupations");
    }

    [Fact]
    public void TaxonomyCreate_Should_SetKind_ForTrainingPrograms()
    {
        var taxonomy = Taxonomy.Create(TaxonomyKind.TrainingPrograms, "Training Programs");
        taxonomy.Kind.Should().Be(TaxonomyKind.TrainingPrograms);
        taxonomy.Name.Should().Be("Training Programs");
    }
}
