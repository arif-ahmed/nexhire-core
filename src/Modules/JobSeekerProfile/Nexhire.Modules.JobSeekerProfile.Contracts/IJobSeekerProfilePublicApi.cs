using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Contracts;

public interface IJobSeekerProfilePublicApi
{
    Task<Result<ProfileCompletenessDto>> GetCompletenessScoreAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ProfileVerificationDto>> GetVerificationStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ProfilePublicDto?>> GetPublicProfileAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record ProfileCompletenessDto(int Percentage, IReadOnlyCollection<string> MissingSections);

public record ProfileVerificationDto(bool IdentityVerified, bool EducationVerified, bool SelfAttested);

public record ProfilePublicDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Headline,
    IReadOnlyCollection<PublicSkillDto> Skills,
    IReadOnlyCollection<PublicExperienceDto> Experience,
    IReadOnlyCollection<PublicEducationDto> Education,
    string? CurrentCity,
    string? CurrentCountry);

public record PublicSkillDto(string Label, string Category, int Proficiency);

public record PublicExperienceDto(string Company, string Role, DateTime StartDate, DateTime? EndDate);

public record PublicEducationDto(string Degree, string Institution, DateTime StartDate, DateTime? EndDate);
