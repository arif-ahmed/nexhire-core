using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubResumeParser : IResumeParser
{
    public Task<Result<ParsedResumeData>> ParseAsync(
        FileReference file,
        CancellationToken cancellationToken = default)
    {
        var confidence = ConfidenceScore.Create(95).Value;
        var personal = new ParsedPersonal("John", "Doe", "john.doe@example.com", "+8801700000000", confidence);

        var education = new[]
        {
            new ParsedEducation("Bachelor of Science in Computer Science", "University of Dhaka", new DateTime(2018, 1, 1), new DateTime(2022, 1, 1), confidence)
        };

        var experience = new[]
        {
            new ParsedExperience("Tech Solutions", "Software Engineer", new DateTime(2022, 2, 1), null, true, "Developed robust systems.", confidence)
        };

        var skills = new[]
        {
            new ParsedSkill("C#", confidence),
            new ParsedSkill("ASP.NET Core", confidence),
            new ParsedSkill("PostgreSQL", confidence)
        };

        var parsedData = ParsedResumeData.Create(personal, education, experience, skills).Value;
        return Task.FromResult(Result.Success(parsedData));
    }
}
