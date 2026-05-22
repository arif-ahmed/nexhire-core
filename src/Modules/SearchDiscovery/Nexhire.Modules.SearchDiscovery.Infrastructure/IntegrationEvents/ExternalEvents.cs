using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.IntegrationEvents;

// BC-4 Job Postings events (consumed — local copies until shared contracts exist)

public record JobPostingPublishedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId,
    Guid EmployerId,
    string Title,
    string Summary,
    string CompanyName,
    string[] Skills,
    string? EducationRequirement,
    int? ExperienceYears,
    string LocationDistrict,
    string? LocationCity,
    double? LocationLatitude,
    double? LocationLongitude,
    string EmploymentType,
    string WorkFormat,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? SectorIndustry,
    DateTime PostedOnUtc,
    DateTime? ApplicationDeadlineUtc,
    long SourceVersion) : IDomainEvent;

public record JobPostingUpdatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId,
    string? Title,
    string? Summary,
    string[]? Skills,
    string? EducationRequirement,
    int? ExperienceYears,
    string? LocationDistrict,
    string? LocationCity,
    double? LocationLatitude,
    double? LocationLongitude,
    string? EmploymentType,
    string? WorkFormat,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? SectorIndustry,
    DateTime? ApplicationDeadlineUtc,
    long SourceVersion) : IDomainEvent;

public record JobPostingExpiredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId) : IDomainEvent;

public record JobPostingClosedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId) : IDomainEvent;

public record JobPostingSuspendedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId) : IDomainEvent;

public record JobPostingReinstatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId,
    Guid EmployerId,
    string Title,
    string Summary,
    string CompanyName,
    string[] Skills,
    string? EducationRequirement,
    int? ExperienceYears,
    string LocationDistrict,
    string? LocationCity,
    double? LocationLatitude,
    double? LocationLongitude,
    string EmploymentType,
    string WorkFormat,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? SectorIndustry,
    DateTime PostedOnUtc,
    DateTime? ApplicationDeadlineUtc,
    long SourceVersion) : IDomainEvent;

// BC-7 Matching/Recommendation events (consumed)

public record MatchComputedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid JobSeekerId,
    Guid PostingId,
    int Score) : IDomainEvent;

public record RecommendationGeneratedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid JobSeekerId,
    Guid[] PostingIds) : IDomainEvent;

// BC-11 Taxonomy events (consumed)

public record TaxonomyUpdatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    string TaxonomyCode,
    string NewLabel) : IDomainEvent;
