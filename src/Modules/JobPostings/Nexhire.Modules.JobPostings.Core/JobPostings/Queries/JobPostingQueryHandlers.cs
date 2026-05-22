using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.JobPostings.Queries;

public sealed class JobPostingQueryHandlers :
    IQueryHandler<GetMyJobPostingsQuery, IReadOnlyCollection<JobPostingSummaryDto>>,
    IQueryHandler<GetJobPostingByIdQuery, JobPostingDto>,
    IQueryHandler<GetSchemaOrgJobPostingQuery, SchemaOrgJobPostingDto>,
    IQueryHandler<GetPostingAuditTrailQuery, IReadOnlyCollection<AuditEntryDto>>,
    IQueryHandler<AdminListJobPostingsQuery, IReadOnlyCollection<AdminJobPostingListItemDto>>,
    IQueryHandler<AdminGetJobPostingDetailQuery, AdminJobPostingDetailDto>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly IPostingMetricsStore _metrics;

    public JobPostingQueryHandlers(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, IPostingMetricsStore metrics)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _metrics = metrics;
    }

    public async Task<Result<IReadOnlyCollection<JobPostingSummaryDto>>> Handle(GetMyJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var postings = await _postings.GetByEmployerIdAsync(request.EmployerId, request.Status, cancellationToken);
        return Result.Success<IReadOnlyCollection<JobPostingSummaryDto>>(postings.Select(JobPostingMappers.ToSummaryDto).ToArray());
    }

    public async Task<Result<JobPostingDto>> Handle(GetJobPostingByIdQuery request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure<JobPostingDto>(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (request.EmployerId is not null && posting.EmployerId != request.EmployerId) return Result.Failure<JobPostingDto>(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        return Result.Success(JobPostingMappers.ToDto(posting));
    }

    public async Task<Result<SchemaOrgJobPostingDto>> Handle(GetSchemaOrgJobPostingQuery request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure<SchemaOrgJobPostingDto>(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (request.EmployerId is not null && posting.EmployerId != request.EmployerId) return Result.Failure<SchemaOrgJobPostingDto>(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        return posting.SchemaOrg is null
            ? Result.Failure<SchemaOrgJobPostingDto>(new Error("E-POST-SCHEMA-ORG-NOT-FOUND", "Schema.org representation is not available."))
            : Result.Success(JobPostingMappers.ToDto(posting.SchemaOrg));
    }

    public async Task<Result<IReadOnlyCollection<AuditEntryDto>>> Handle(GetPostingAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure<IReadOnlyCollection<AuditEntryDto>>(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (request.EmployerId is not null && posting.EmployerId != request.EmployerId) return Result.Failure<IReadOnlyCollection<AuditEntryDto>>(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        var trail = await _auditTrails.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        return Result.Success<IReadOnlyCollection<AuditEntryDto>>((trail?.Entries ?? Array.Empty<Domain.Aggregates.AuditEntry>()).OrderBy(e => e.OccurredOnUtc).Select(JobPostingMappers.ToDto).ToArray());
    }

    public async Task<Result<IReadOnlyCollection<AdminJobPostingListItemDto>>> Handle(AdminListJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var postings = await _postings.SearchAsync(
            new JobPostingSearchFilter(request.EmployerId, request.Status, request.PostedFromUtc, request.PostedToUtc, request.Location, request.Query),
            cancellationToken);

        return Result.Success<IReadOnlyCollection<AdminJobPostingListItemDto>>(postings.Select(JobPostingMappers.ToAdminListItemDto).ToArray());
    }

    public async Task<Result<AdminJobPostingDetailDto>> Handle(AdminGetJobPostingDetailQuery request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure<AdminJobPostingDetailDto>(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        var metrics = await _metrics.GetAsync(posting.Id, cancellationToken);
        return Result.Success(new AdminJobPostingDetailDto(JobPostingMappers.ToDto(posting), metrics?.ApplicationsCount ?? 0, metrics?.MatchesCount ?? 0, metrics?.ViewsCount ?? 0));
    }
}
