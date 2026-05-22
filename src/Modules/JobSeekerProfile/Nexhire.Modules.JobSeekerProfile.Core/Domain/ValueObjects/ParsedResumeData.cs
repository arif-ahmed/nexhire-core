using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public record ParsedPersonal(string? FirstName, string? LastName, string? Email, string? Mobile, ConfidenceScore Confidence);

public record ParsedEducation(string Degree, string Institution, DateTime? StartDate, DateTime? EndDate, ConfidenceScore Confidence);

public record ParsedExperience(string Company, string Role, DateTime? StartDate, DateTime? EndDate, bool IsCurrent, string Responsibilities, ConfidenceScore Confidence);

public record ParsedSkill(string RawLabel, ConfidenceScore Confidence);

public class ParsedResumeData : ValueObject
{
    public ParsedPersonal Personal { get; }
    public IReadOnlyCollection<ParsedEducation> Education { get; }
    public IReadOnlyCollection<ParsedExperience> Experience { get; }
    public IReadOnlyCollection<ParsedSkill> Skills { get; }

    private ParsedResumeData(
        ParsedPersonal personal,
        IReadOnlyCollection<ParsedEducation> education,
        IReadOnlyCollection<ParsedExperience> experience,
        IReadOnlyCollection<ParsedSkill> skills)
    {
        Personal = personal;
        Education = education;
        Experience = experience;
        Skills = skills;
    }

    public static Result<ParsedResumeData> Create(
        ParsedPersonal personal,
        IEnumerable<ParsedEducation> education,
        IEnumerable<ParsedExperience> experience,
        IEnumerable<ParsedSkill> skills)
    {
        if (personal == null)
        {
            return Result.Failure<ParsedResumeData>(new Error("ParsedResumeData.NullPersonal", "Personal information cannot be null."));
        }

        return Result.Success(new ParsedResumeData(
            personal,
            education?.ToList().AsReadOnly() ?? new List<ParsedEducation>().AsReadOnly(),
            experience?.ToList().AsReadOnly() ?? new List<ParsedExperience>().AsReadOnly(),
            skills?.ToList().AsReadOnly() ?? new List<ParsedSkill>().AsReadOnly()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Personal;
        foreach (var edu in Education) yield return edu;
        foreach (var exp in Experience) yield return exp;
        foreach (var skill in Skills) yield return skill;
    }
}
