using FluentAssertions;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Events;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit;

public class AggregateTests
{
    private static PersonName GetValidName() => PersonName.Create("John", "Doe").Value;
    private static EmailAddress GetValidEmail() => EmailAddress.Create("john.doe@nexhire.com").Value;
    private static MobileNumber GetValidMobile() => MobileNumber.Create("+8801712345678").Value;

    [Fact]
    public void Register_Should_CreateProfileInPendingActivation_AndSetL1Fields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = GetValidName();
        var email = GetValidEmail();
        var mobile = GetValidMobile();

        // Act
        var result = Aggregates.JobSeekerProfile.Register(id, userId, name, email, mobile, Gender.Male);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var profile = result.Value;
        profile.Id.Should().Be(id);
        profile.UserId.Should().Be(userId);
        profile.Status.Should().Be(ProfileStatus.PendingActivation);
        profile.Name.Should().Be(name);
        profile.Email.Should().Be(email);
        profile.Mobile.Should().Be(mobile);
        profile.Gender.Should().Be(Gender.Male);
        profile.Completeness.Percentage.Should().Be(30); // L1 only = 30%
        profile.DomainEvents.Should().ContainSingle(e => e is JobSeekerRegisteredEvent);
    }

    [Fact]
    public void StatusTransitions_Should_EnforceStateMachineCorrectly()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;

        // 1. PendingActivation -> Active (Success)
        var actResult1 = profile.Activate();
        actResult1.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Active);
        profile.DomainEvents.Should().Contain(e => e is ProfileActivatedEvent);

        // 2. Idempotent Activation (Success, no change)
        var actResult2 = profile.Activate();
        actResult2.IsSuccess.Should().BeTrue();

        // 3. Active -> Deactivated (Success)
        var deactResult = profile.Deactivate();
        deactResult.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Deactivated);
        profile.DomainEvents.Should().Contain(e => e is ProfileDeactivatedEvent);

        // 4. Deactivated -> Active (Success)
        var reactResult = profile.Reactivate();
        reactResult.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Active);
        profile.DomainEvents.Should().Contain(e => e is ProfileReactivatedEvent);
    }

    [Fact]
    public void ActivateFromDeactivated_Should_Fail()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        profile.Activate();
        profile.Deactivate();

        // Act
        var result = profile.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Profile.ActivateFromDeactivated");
    }

    [Fact]
    public void ReactivateFromPendingActivation_Should_Fail()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;

        // Act
        var result = profile.Reactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Profile.ReactivateInvalidStatus");
    }

    [Fact]
    public void AddEducation_Should_RaiseLevel2CompletedEvent_OnFirstEntry()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var period = DateRange.Create(DateTime.UtcNow.AddYears(-4), DateTime.UtcNow.AddYears(-1)).Value;

        // Act
        var result = profile.AddEducation("Bachelor of Science", "University of Dhaka", period, 3.8m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Education.Should().HaveCount(1);
        profile.IsLevel2Complete.Should().BeTrue();
        profile.Completeness.Percentage.Should().Be(40); // 30 + 10 (Education)
        profile.DomainEvents.Should().ContainSingle(e => e is ProfileLevel2CompletedEvent);

        // Clear events and add another education to verify event is NOT raised again
        profile.ClearDomainEvents();
        var result2 = profile.AddEducation("Master of Science", "University of Dhaka", period, 3.9m);
        result2.IsSuccess.Should().BeTrue();
        profile.DomainEvents.Should().NotContain(e => e is ProfileLevel2CompletedEvent);
        profile.DomainEvents.Should().NotContain(e => e is ProfileCompletenessChangedEvent); // Percentage remains 40%
    }

    [Fact]
    public void AddExperience_WithIsCurrentTrue_Should_RequireNullEndDate()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var start = DateTime.UtcNow.AddYears(-1);

        // Act & Assert
        // 1. IsCurrent but has end date -> Should fail
        var periodWithEnd = DateRange.Create(start, DateTime.UtcNow).Value;
        var result1 = ExperienceEntry.Create(Guid.NewGuid(), "TechCorp", "Developer", periodWithEnd, true, "Coding");
        result1.IsFailure.Should().BeTrue();
        result1.Error.Code.Should().Be("ExperienceEntry.InvalidCurrentEndDate");

        // 2. IsCurrent and null end date -> Should succeed
        var periodNullEnd = DateRange.Create(start, null).Value;
        var result2 = profile.AddExperience("TechCorp", "Developer", periodNullEnd, true, "Coding");
        result2.IsSuccess.Should().BeTrue();
        profile.Experience.Should().HaveCount(1);
    }

    [Fact]
    public void AddSkill_Should_PreventDuplicates_AndValidateProficiency()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var canonicalRef = CanonicalSkillRef.Create("SK-CS-001", "C# Programming").Value;

        // Act & Assert
        // 1. Invalid proficiency (< 1)
        var skillErr1 = ProfileSkill.Create(Guid.NewGuid(), canonicalRef, "C#", SkillCategory.Hard, SkillTier.Primary, 0);
        skillErr1.IsFailure.Should().BeTrue();
        skillErr1.Error.Code.Should().Be("ProfileSkill.InvalidProficiency");

        // 2. Invalid proficiency (> 5)
        var skillErr2 = ProfileSkill.Create(Guid.NewGuid(), canonicalRef, "C#", SkillCategory.Hard, SkillTier.Primary, 6);
        skillErr2.IsFailure.Should().BeTrue();

        // 3. Add Valid Skill
        var addResult1 = profile.AddSkill(canonicalRef, "C#", SkillCategory.Hard, SkillTier.Primary, 4);
        addResult1.IsSuccess.Should().BeTrue();
        profile.Skills.Should().HaveCount(1);

        // 4. Add Duplicate Skill (fails)
        var addResult2 = profile.AddSkill(canonicalRef, "C# Compiler", SkillCategory.Hard, SkillTier.Secondary, 3);
        addResult2.IsFailure.Should().BeTrue();
        addResult2.Error.Code.Should().Be("ProfileSkill.DuplicateSkill");
    }

    [Fact]
    public void SupplementaryDocuments_Should_EnforceLimitOf10_AndRejectInfected()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var cleanScan = VirusScanResult.Create(VirusScanStatus.Clean, DateTime.UtcNow).Value;
        var infectedScan = VirusScanResult.Create(VirusScanStatus.Infected, DateTime.UtcNow).Value;

        // Act & Assert
        // 1. Reject infected file
        var fileInfected = FileReference.Create("key_inf", "virus.png", "image/png", 500).Value;
        var docInfectedResult = SupplementaryDocument.Create(Guid.NewGuid(), fileInfected, DocumentKind.Portfolio, infectedScan);
        docInfectedResult.IsFailure.Should().BeTrue();
        docInfectedResult.Error.Code.Should().Be("E-UPLOAD-VIRUS");

        // 2. Add 10 clean documents
        for (int i = 1; i <= 10; i++)
        {
            var file = FileReference.Create($"key_{i}", $"doc{i}.pdf", "application/pdf", 1024).Value;
            var addResult = profile.AddSupplementaryDocument(file, DocumentKind.Certificate, cleanScan);
            addResult.IsSuccess.Should().BeTrue();
        }
        profile.Documents.Should().HaveCount(10);
        profile.Completeness.Percentage.Should().Be(40); // L1 (30) + L3 Documents (10) = 40%

        // 3. Add 11th document (should fail)
        var file11 = FileReference.Create("key_11", "doc11.pdf", "application/pdf", 1024).Value;
        var addResult11 = profile.AddSupplementaryDocument(file11, DocumentKind.Certificate, cleanScan);
        addResult11.IsFailure.Should().BeTrue();
        addResult11.Error.Code.Should().Be("E-UPLOAD-LIMIT-EXCEEDED");
    }

    [Fact]
    public void EnablePublicSharing_Should_OnlySucceedAt100PercentCompleteness()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var qrCode = FileReference.Create("qr_key", "qr.png", "image/png", 2048).Value;

        // 1. Try enabling when incomplete (30% complete) -> fails
        profile.Activate();
        var resultIncomplete = profile.EnablePublicSharing("john-doe-1234", qrCode);
        resultIncomplete.IsFailure.Should().BeTrue();
        resultIncomplete.Error.Code.Should().Be("E-SHARE-PROFILE-INCOMPLETE");

        // 2. Force complete profile to 100%
        var period = DateRange.Create(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddYears(-1)).Value;
        profile.AddEducation("B.Sc.", "DU", period, 4.0m); // L2 milestone 1 (+10)
        profile.AddExperience("A", "Dev", period, false, "Code"); // L2 milestone 2 (+10)
        profile.AddSkill(CanonicalSkillRef.Create("SK-1", "C#").Value, "C#", SkillCategory.Hard, SkillTier.Primary, 5); // L2 milestone 3 (+10)
        profile.SetPreferences(JobPreferences.Create(new[] { "Full-time" }, new[] { "Tech" }, new[] { "Dhaka" }, new[] { WorkArrangement.Remote }, null).Value); // L2 milestone 4 (+10)
        profile.SetAddresses(Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value, null); // L2 milestone 5 (+10)
        profile.AddSupplementaryDocument(FileReference.Create("k", "c.pdf", "application/pdf", 100).Value, DocumentKind.Certificate, VirusScanResult.Create(VirusScanStatus.Clean).Value); // L3 (+10)
        profile.MarkResumeAttached(); // Resume (+10)

        profile.Completeness.Percentage.Should().Be(100);

        // 3. Try sharing when 100% complete but inactive (e.g. pending activation or deactivated)
        var profilePending = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        // Mock 100% completeness for pending profile
        profilePending.RestoreSnapshot(
            profile.Name, profile.Email, profile.Mobile, profile.Gender,
            profile.Education, profile.Experience, profile.Skills, profile.Documents,
            profile.Preferences, profile.CurrentAddress, profile.PermanentAddress,
            profile.RecentSalary, profile.Visibility, profile.PublicSharing, profile.Verification,
            profile.HasActiveResume);
        profilePending.Completeness.Percentage.Should().Be(100);
        profilePending.Status.Should().Be(ProfileStatus.PendingActivation);

        var resultPending = profilePending.EnablePublicSharing("john-doe-1234", qrCode);
        resultPending.IsFailure.Should().BeTrue();
        resultPending.Error.Code.Should().Be("Profile.InactiveForSharing");

        // 4. Try sharing when Active and 100% complete -> succeeds
        var resultSucceeds = profile.EnablePublicSharing("john-doe-1234", qrCode);
        resultSucceeds.IsSuccess.Should().BeTrue();
        profile.PublicSharing.Enabled.Should().BeTrue();
        profile.PublicSharing.Slug.Should().Be("john-doe-1234");
        profile.PublicSharing.QrCodeRef.Should().Be(qrCode);

        // 5. Deactivation forces public sharing disabled
        profile.Deactivate();
        profile.PublicSharing.Enabled.Should().BeFalse();
        profile.PublicSharing.Slug.Should().BeNull();
    }

    [Fact]
    public void Resume_Upload_Should_ValidateMimeAndSize()
    {
        // Act & Assert
        // 1. Valid PDF
        var filePdf = FileReference.Create("key", "cv.pdf", "application/pdf", 1024).Value;
        var resumeResult = Resume.Upload(Guid.NewGuid(), Guid.NewGuid(), filePdf);
        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Value.ParseStatus.Should().Be(ResumeParseStatus.Uploaded);

        // 2. Invalid Mime (image)
        var filePng = FileReference.Create("key", "pic.png", "image/png", 1024).Value;
        var resumeErr1 = Resume.Upload(Guid.NewGuid(), Guid.NewGuid(), filePng);
        resumeErr1.IsFailure.Should().BeTrue();
        resumeErr1.Error.Code.Should().Be("E-UPLOAD-INVALID-FORMAT");

        // 3. Exceeded Size
        var fileLarge = FileReference.Create("key", "cv.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 6 * 1024 * 1024).Value;
        var resumeErr2 = Resume.Upload(Guid.NewGuid(), Guid.NewGuid(), fileLarge);
        resumeErr2.IsFailure.Should().BeTrue();
        resumeErr2.Error.Code.Should().Be("E-UPLOAD-SIZE-EXCEEDED");
    }

    [Fact]
    public void Resume_ScanResult_Should_UpdateStateCorrectly()
    {
        // Arrange
        var filePdf = FileReference.Create("key", "cv.pdf", "application/pdf", 1024).Value;
        
        // 1. Infected Scan Result
        var resumeInfected = Resume.Upload(Guid.NewGuid(), Guid.NewGuid(), filePdf).Value;
        var infectedResult = VirusScanResult.Create(VirusScanStatus.Infected, DateTime.UtcNow).Value;
        
        var scanInfected = resumeInfected.RecordScanResult(infectedResult);
        scanInfected.IsFailure.Should().BeTrue();
        resumeInfected.ParseStatus.Should().Be(ResumeParseStatus.Failed);
        resumeInfected.FailureReason.Should().Be("Virus detected in file");
        resumeInfected.DomainEvents.Should().ContainSingle(e => e is ResumeScanFailedEvent);

        // 2. Clean Scan Result
        var resumeClean = Resume.Upload(Guid.NewGuid(), Guid.NewGuid(), filePdf).Value;
        var cleanResult = VirusScanResult.Create(VirusScanStatus.Clean, DateTime.UtcNow).Value;

        var scanClean = resumeClean.RecordScanResult(cleanResult);
        scanClean.IsSuccess.Should().BeTrue();
        resumeClean.ParseStatus.Should().Be(ResumeParseStatus.Scanned);
    }

    [Fact]
    public void ProfileHistory_Should_SupportAppendAndPurge()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var history = ProfileHistory.Start(Guid.NewGuid(), profileId).Value;

        // Act
        history.AppendEdit("{\"Name\":\"John\"}", new[] { "Name" });
        history.AppendEdit("{\"Mobile\":\"+880\"}", new[] { "Mobile" });
        history.AppendRestore("{\"Name\":\"John Doe\"}", Guid.NewGuid());

        // Assert
        history.Versions.Should().HaveCount(3);
        history.Versions.Should().Contain(v => v.Action == HistoryAction.Edited);
        history.Versions.Should().Contain(v => v.Action == HistoryAction.Restored);

        // Let's check history purge
        history.PurgeOlderThan(DateTime.UtcNow.AddMinutes(5));
        history.Versions.Should().BeEmpty();
    }

    [Fact]
    public void ProfileCompletenessCalculator_Should_CalculateScoreCorrectly()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;

        // 1. Initial registered profile
        profile.Completeness.Percentage.Should().Be(30);
        profile.Completeness.MissingSections.Should().Contain(new[] { "Education", "Experience", "Skills", "Job Preferences", "Current Address", "Supplementary Documents", "Active Resume" });

        // 2. Add education
        var period = DateRange.Create(DateTime.UtcNow.AddYears(-1), null).Value;
        profile.AddEducation("B.Sc.", "Uni", period, 3.5m);
        profile.Completeness.Percentage.Should().Be(40);
        profile.Completeness.MissingSections.Should().NotContain("Education");

        // 3. Add experience
        profile.AddExperience("A", "Dev", period, true, "Responsibilities");
        profile.Completeness.Percentage.Should().Be(50);
        profile.Completeness.MissingSections.Should().NotContain("Experience");

        // 4. Set current address
        var address = Address.Create("123 St", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        profile.SetAddresses(address, null);
        profile.Completeness.Percentage.Should().Be(60);

        // 5. Set Preferences
        var prefs = JobPreferences.Create(new[] { "Full-time" }, new[] { "Tech" }, new[] { "Dhaka" }, new[] { WorkArrangement.Remote }, null).Value;
        profile.SetPreferences(prefs);
        profile.Completeness.Percentage.Should().Be(70);

        // 6. Set Skills
        var skillRef = CanonicalSkillRef.Create("SK-1", "C#").Value;
        profile.AddSkill(skillRef, "C#", SkillCategory.Hard, SkillTier.Primary, 5);
        profile.Completeness.Percentage.Should().Be(80);

        // 7. Add Supplementary Document
        var file = FileReference.Create("k", "d.pdf", "application/pdf", 1024).Value;
        profile.AddSupplementaryDocument(file, DocumentKind.Certificate, VirusScanResult.Create(VirusScanStatus.Clean).Value);
        profile.Completeness.Percentage.Should().Be(90);

        // 8. Mark Active Resume
        profile.MarkResumeAttached();
        profile.Completeness.Percentage.Should().Be(100);
    }

    [Fact]
    public void PublicSlugGenerator_Should_GenerateSlugWithRetries_AndCleanNameAndProfanity()
    {
        // Arrange
        var name = PersonName.Create("Jóhn-Ass!", "Dôe").Value;

        // Act
        var result = PublicSlugGenerator.Generate(name, slug => false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var nameClean = PersonName.Create("Alice", "Smith").Value;
        var resultClean = PublicSlugGenerator.Generate(nameClean, slug => false);
        resultClean.IsSuccess.Should().BeTrue();
        resultClean.Value.Should().StartWith("alice-smith-");
        resultClean.Value.Length.Should().Be("alice-smith-".Length + 4);

        // 2. Uniqueness Collision Check
        var calledCount = 0;
        var collisionResult = PublicSlugGenerator.Generate(nameClean, slug =>
        {
            calledCount++;
            return calledCount < 3;
        });
        collisionResult.IsSuccess.Should().BeTrue();
        calledCount.Should().Be(3);
    }

    [Fact]
    public void ResumeReplacementService_Should_SupersedeExistingActiveResume()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var filePdf = FileReference.Create("key", "cv.pdf", "application/pdf", 1024).Value;
        var resumeExisting = Resume.Upload(Guid.NewGuid(), profileId, filePdf).Value;
        var resumeNew = Resume.Upload(Guid.NewGuid(), profileId, filePdf).Value;

        // Act
        var replaceResult = ResumeReplacementService.Replace(resumeExisting, resumeNew);

        // Assert
        replaceResult.IsSuccess.Should().BeTrue();
        resumeExisting.IsSuperseded.Should().BeTrue();
        resumeNew.IsSuperseded.Should().BeFalse();
    }

    [Fact]
    public void ResumeToProfileMerger_Should_MergeSelectedFields_AndAggregateErrors()
    {
        // Arrange
        var profile = Aggregates.JobSeekerProfile.Register(Guid.NewGuid(), Guid.NewGuid(), GetValidName(), GetValidEmail(), GetValidMobile(), Gender.Male).Value;
        var personal = new ParsedPersonal("John", "Doe", "john.doe@nexhire.com", "+8801712345678", ConfidenceScore.Create(95).Value);
        var edu = new ParsedEducation("Bachelor of Science", "University of Dhaka", DateTime.UtcNow.AddYears(-4), DateTime.UtcNow.AddYears(-1), ConfidenceScore.Create(90).Value);
        var exp1 = new ParsedExperience("TechCorp", "Developer", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, false, "Coding", ConfidenceScore.Create(88).Value);
        var expInvalid = new ParsedExperience("BadCorp", "Developer", DateTime.UtcNow, DateTime.UtcNow.AddYears(-1), false, "Buggy", ConfidenceScore.Create(92).Value);
        var skill1 = new ParsedSkill("C# Programming", ConfidenceScore.Create(85).Value);
        var skill2 = new ParsedSkill("UnknownSkill", ConfidenceScore.Create(80).Value);

        var parsed = ParsedResumeData.Create(
            personal,
            new[] { edu },
            new[] { exp1, expInvalid },
            new[] { skill1, skill2 }).Value;

        Func<string, Result<CanonicalSkillRef>> mapSkill = label =>
        {
            if (label == "C# Programming")
            {
                return Result.Success(CanonicalSkillRef.Create("SK-CS-001", "C# Programming").Value);
            }
            return Result.Failure<CanonicalSkillRef>(new Error("Taxonomy.NotFound", $"Skill '{label}' not found in taxonomy."));
        };

        // Act
        var mergeKeys = new[] { "Education", "experience:0", "experience:1", "skill:0", "skill:1" };
        var result = ResumeToProfileMerger.MergeSelectedFields(profile, parsed, mergeKeys, mapSkill);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Merge.PartialFailure");
        result.Error.Message.Should().Contain("Start date must be less than or equal to end date");
        result.Error.Message.Should().Contain("Skill 'UnknownSkill' not found in taxonomy");

        profile.Education.Should().HaveCount(1);
        profile.Experience.Should().HaveCount(1);
        profile.Experience.First().Company.Should().Be("TechCorp");
        profile.Skills.Should().HaveCount(1);
        profile.Skills.First().CanonicalSkillRef.TaxonomyCode.Should().Be("SK-CS-001");
    }
}
