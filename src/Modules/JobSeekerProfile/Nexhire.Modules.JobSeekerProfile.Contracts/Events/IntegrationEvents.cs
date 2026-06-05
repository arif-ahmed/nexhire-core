using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobSeekerProfile.Contracts.Events;

public record JobSeekerRegisteredIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileLevel2CompletedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, int CompletenessPercentage, DateTime OccurredOnUtc) : IDomainEvent;

public record ResumeUploadedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, Guid ResumeId, string MimeType, DateTime OccurredOnUtc) : IDomainEvent;

public record SkillPayload(string TaxonomyCode, string Label, int Confidence);

public record ParsedEducationPayload(string Degree, string Institution, DateTime? Start, DateTime? End);

public record ParsedExperiencePayload(string Role, string Company, DateTime? Start, DateTime? End);

public record ResumeParsedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, Guid ResumeId,
    IReadOnlyCollection<SkillPayload> Skills,
    IReadOnlyCollection<ParsedEducationPayload> Education,
    IReadOnlyCollection<ParsedExperiencePayload> Experience,
    DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileSkillsUpdatedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId,
    IReadOnlyCollection<string> AddedSkills,
    IReadOnlyCollection<string> RemovedSkills,
    DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileVisibilityChangedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, string Visibility, DateTime OccurredOnUtc) : IDomainEvent;

public record SupplementaryDocumentUploadedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, Guid SupplementaryDocumentId, string Kind, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileCompletenessChangedIntegrationEvent(
    Guid EventId, Guid JobSeekerProfileId, int Score, DateTime OccurredOnUtc) : IDomainEvent;
