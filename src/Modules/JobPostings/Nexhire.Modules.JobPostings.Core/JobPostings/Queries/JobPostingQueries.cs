using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobPostings.Core.JobPostings.Queries;

public sealed record GetMyJobPostingsQuery(Guid EmployerId, PostingStatus? Status) : IQuery<IReadOnlyCollection<JobPostingSummaryDto>>;
public sealed record GetJobPostingByIdQuery(Guid JobPostingId, Guid? EmployerId = null) : IQuery<JobPostingDto>;
public sealed record GetSchemaOrgJobPostingQuery(Guid JobPostingId, Guid? EmployerId = null) : IQuery<SchemaOrgJobPostingDto>;
public sealed record GetPostingAuditTrailQuery(Guid JobPostingId, Guid? EmployerId = null) : IQuery<IReadOnlyCollection<AuditEntryDto>>;
public sealed record AdminListJobPostingsQuery(Guid? EmployerId, PostingStatus? Status, DateTime? PostedFromUtc, DateTime? PostedToUtc, string? Location, string? Query) : IQuery<IReadOnlyCollection<AdminJobPostingListItemDto>>;
public sealed record AdminGetJobPostingDetailQuery(Guid JobPostingId) : IQuery<AdminJobPostingDetailDto>;
