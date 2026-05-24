using MediatR;
using Nexhire.Modules.JobPostings.Core.DTOs;

namespace Nexhire.Modules.JobPostings.Core.IntegrationEvents;

public sealed record EmployerVerifiedIntegrationEvent(Guid EmployerId, DateTime VerifiedOnUtc) : INotification;
public sealed record EmployerVerificationFailedIntegrationEvent(Guid EmployerId, string Reason, DateTime OccurredOnUtc) : INotification;
public sealed record EmployerAccountDeactivatedIntegrationEvent(Guid EmployerId, DateTime DeactivatedOnUtc) : INotification;
public sealed record EmployerAccountSuspendedIntegrationEvent(Guid EmployerId, string Reason, DateTime OccurredOnUtc) : INotification;
public sealed record EmployerAccountReinstatedIntegrationEvent(Guid EmployerId, DateTime OccurredOnUtc) : INotification;
public sealed record ExternalJobIngestedIntegrationEvent(string ExternalRef, string PartnerId, Guid EmployerId, Guid PostedByUserId, JobPostingDraftDto NormalizedPosting, DateTime OccurredOnUtc) : INotification;
public sealed record TaxonomyUpdatedIntegrationEvent(IReadOnlyCollection<string> DeprecatedSkillCodes, DateTime OccurredOnUtc) : INotification;
public sealed record JobPostingApplicationsCountChangedIntegrationEvent(Guid JobPostingId, int ApplicationsCount, DateTime OccurredOnUtc) : INotification;
public sealed record JobPostingMatchesCountChangedIntegrationEvent(Guid JobPostingId, int MatchesCount, DateTime OccurredOnUtc) : INotification;
public sealed record JobPostingViewsCountChangedIntegrationEvent(Guid JobPostingId, int ViewsCount, DateTime OccurredOnUtc) : INotification;
public sealed record EmployerRegisteredIntegrationEvent(Guid EventId, Guid EmployerProfileId, Guid UserId, string CompanyName, DateTime At, DateTime OccurredOnUtc) : INotification;
