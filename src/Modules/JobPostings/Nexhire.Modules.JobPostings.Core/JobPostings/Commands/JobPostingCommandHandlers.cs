using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.Services;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.JobPostings.Commands;

public sealed class CreateJobPostingCommandHandler : ICommandHandler<CreateJobPostingCommand, Guid>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public CreateJobPostingCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, ITaxonomyApi taxonomyApi, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _taxonomyApi = taxonomyApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
    {
        var details = await JobPostingDraftFactory.BuildAsync(request.Draft, _taxonomyApi, cancellationToken);
        if (details.IsFailure) return Result.Failure<Guid>(details.Error);

        var posting = JobPosting.CreateDraft(
            request.EmployerId,
            request.PostedByUserId,
            details.Value.Title,
            details.Value.Summary,
            details.Value.ContractType,
            details.Value.EducationLevel,
            details.Value.WorkFormat,
            details.Value.Location,
            details.Value.RequiredSkills,
            details.Value.RequiredLanguages,
            details.Value.Deadline,
            details.Value.JobLink,
            details.Value.SalaryRange,
            details.Value.Visibility);

        if (posting.IsFailure) return Result.Failure<Guid>(posting.Error);

        await _postings.AddAsync(posting.Value, cancellationToken);
        await _auditTrails.AddAsync(PostingAuditTrail.Create(posting.Value.Id), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(posting.Value.Id);
    }
}

public sealed class PublishJobPostingCommandHandler : ICommandHandler<PublishJobPostingCommand>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly IEmployerStandingStore _employerStanding;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly SchemaOrgStandardizer _schemaOrgStandardizer;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public PublishJobPostingCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, IEmployerStandingStore employerStanding, ITaxonomyApi taxonomyApi, SchemaOrgStandardizer schemaOrgStandardizer, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _employerStanding = employerStanding;
        _taxonomyApi = taxonomyApi;
        _schemaOrgStandardizer = schemaOrgStandardizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PublishJobPostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));

        foreach (var skill in posting.RequiredSkills)
        {
            if (!await _taxonomyApi.IsValidSkillCodeAsync(skill.CanonicalRef.TaxonomyCode, cancellationToken))
            {
                return Result.Failure(new Error("E-POST-INVALID-SKILL-CODE", "One or more skill codes are invalid."));
            }
        }

        var standing = await _employerStanding.GetAsync(posting.EmployerId, cancellationToken) ?? EmployerStanding.Ineligible(posting.EmployerId);
        var from = posting.Status;
        var publish = posting.Publish(_schemaOrgStandardizer.Standardize(posting), standing);
        if (publish.IsFailure) return publish;

        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        var actor = AuditActor.Create(AuditActorKind.Employer, request.UserId, "Employer").Value;
        audit.RecordStatusChange(actor, StatusTransition.Create(from, posting.Status).Value, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class UpdateJobPostingDetailsCommandHandler : ICommandHandler<UpdateJobPostingDetailsCommand>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly SchemaOrgStandardizer _schemaOrgStandardizer;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public UpdateJobPostingDetailsCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, ITaxonomyApi taxonomyApi, SchemaOrgStandardizer schemaOrgStandardizer, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _taxonomyApi = taxonomyApi;
        _schemaOrgStandardizer = schemaOrgStandardizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateJobPostingDetailsCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));

        var details = await JobPostingDraftFactory.BuildAsync(request.Draft, _taxonomyApi, cancellationToken);
        if (details.IsFailure) return Result.Failure(details.Error);

        var edited = posting.EditDetails(
            details.Value.Title,
            details.Value.Summary,
            details.Value.ContractType,
            details.Value.EducationLevel,
            details.Value.WorkFormat,
            details.Value.Location,
            details.Value.RequiredSkills,
            details.Value.RequiredLanguages,
            details.Value.Deadline,
            details.Value.JobLink,
            details.Value.SalaryRange,
            details.Value.Visibility,
            posting.Status == PostingStatus.Active ? _schemaOrgStandardizer.Standardize(posting) : null);

        if (edited.IsFailure) return Result.Failure(edited.Error);

        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        var actor = AuditActor.Create(AuditActorKind.Employer, posting.PostedByUserId, "Employer").Value;
        audit.RecordFieldEdit(actor, edited.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class SimpleStatusCommandHandler :
    ICommandHandler<PauseJobPostingCommand>,
    ICommandHandler<ResumeJobPostingCommand>,
    ICommandHandler<ArchiveJobPostingCommand>,
    ICommandHandler<SuspendJobPostingCommand>,
    ICommandHandler<ReinstateJobPostingCommand>,
    ICommandHandler<RemoveJobPostingCommand>,
    ICommandHandler<ExtendApplicationDeadlineCommand>,
    ICommandHandler<SetPostingVisibilityCommand>,
    ICommandHandler<UpdateRequiredSkillsCommand>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly SchemaOrgStandardizer _schemaOrgStandardizer;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public SimpleStatusCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, ITaxonomyApi taxonomyApi, SchemaOrgStandardizer schemaOrgStandardizer, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _taxonomyApi = taxonomyApi;
        _schemaOrgStandardizer = schemaOrgStandardizer;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(PauseJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeEmployerStatus(request.JobPostingId, request.EmployerId, request.UserId, AuditActorKind.Employer, p => p.Pause(), cancellationToken);

    public Task<Result> Handle(ResumeJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeEmployerStatus(request.JobPostingId, request.EmployerId, request.UserId, AuditActorKind.Employer, p => p.Resume(), cancellationToken);

    public Task<Result> Handle(ArchiveJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeEmployerStatus(request.JobPostingId, request.EmployerId, request.UserId, AuditActorKind.Employer, p => p.Archive(), cancellationToken);

    public Task<Result> Handle(SuspendJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeAdminStatus(request.JobPostingId, request.AdminUserId, p => p.Suspend(request.Reason), request.Reason, cancellationToken);

    public Task<Result> Handle(ReinstateJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeAdminStatus(request.JobPostingId, request.AdminUserId, p => p.Reinstate(), null, cancellationToken);

    public Task<Result> Handle(RemoveJobPostingCommand request, CancellationToken cancellationToken) =>
        ChangeAdminStatus(request.JobPostingId, request.AdminUserId, p => p.Remove(request.Reason), request.Reason, cancellationToken);

    public async Task<Result> Handle(ExtendApplicationDeadlineCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        var deadline = ApplicationDeadline.Create(request.NewDeadlineUtc, request.AutoCloseEnabled);
        if (deadline.IsFailure) return Result.Failure(deadline.Error);
        var result = posting.ExtendDeadline(deadline.Value);
        if (result.IsFailure) return result;
        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        audit.RecordFieldEdit(AuditActor.Create(AuditActorKind.Employer, posting.PostedByUserId, "Employer").Value, new[] { nameof(JobPosting.Deadline) });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(SetPostingVisibilityCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        var visibility = JobPostingMappers.ToVisibility(request.Visibility);
        if (visibility.IsFailure) return Result.Failure(visibility.Error);
        var result = posting.SetVisibility(visibility.Value);
        if (result.IsFailure) return result;
        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        audit.RecordFieldEdit(AuditActor.Create(AuditActorKind.Employer, posting.PostedByUserId, "Employer").Value, new[] { nameof(JobPosting.Visibility) });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(UpdateRequiredSkillsCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));

        var skills = new List<RequiredSkill>();
        foreach (var input in request.RequiredSkills)
        {
            var canonical = await _taxonomyApi.CanonicalizeSkillAsync(input.RawLabelOrCode, cancellationToken);
            if (canonical.IsFailure) return Result.Failure(canonical.Error);
            var skill = RequiredSkill.Create(canonical.Value, input.RawLabelOrCode, input.Importance);
            if (skill.IsFailure) return Result.Failure(skill.Error);
            skills.Add(skill.Value);
        }

        var result = posting.UpdateRequiredSkills(skills, posting.Status == PostingStatus.Active ? _schemaOrgStandardizer.Standardize(posting) : null);
        if (result.IsFailure) return Result.Failure(result.Error);
        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        audit.RecordFieldEdit(AuditActor.Create(AuditActorKind.Employer, posting.PostedByUserId, "Employer").Value, result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ChangeEmployerStatus(Guid postingId, Guid employerId, Guid userId, AuditActorKind actorKind, Func<JobPosting, Result> change, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(postingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != employerId) return Result.Failure(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        var from = posting.Status;
        var result = change(posting);
        if (result.IsFailure) return result;
        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        audit.RecordStatusChange(AuditActor.Create(actorKind, userId, actorKind.ToString()).Value, StatusTransition.Create(from, posting.Status).Value, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ChangeAdminStatus(Guid postingId, Guid adminUserId, Func<JobPosting, Result> change, string? reason, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(postingId, cancellationToken);
        if (posting is null) return Result.Failure(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        var from = posting.Status;
        var result = change(posting);
        if (result.IsFailure) return result;
        var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
        audit.RecordStatusChange(AuditActor.Create(AuditActorKind.Admin, adminUserId, "Admin").Value, StatusTransition.Create(from, posting.Status).Value, reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

}

public sealed class RenewJobPostingCommandHandler : ICommandHandler<RenewJobPostingCommand, Guid>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly JobPostingRenewalService _renewalService;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public RenewJobPostingCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, JobPostingRenewalService renewalService, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _renewalService = renewalService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RenewJobPostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postings.GetByIdAsync(request.JobPostingId, cancellationToken);
        if (posting is null) return Result.Failure<Guid>(new Error("E-POST-NOT-FOUND", "Job posting was not found."));
        if (posting.EmployerId != request.EmployerId) return Result.Failure<Guid>(new Error("E-POST-FORBIDDEN", "Posting does not belong to this employer."));
        var deadline = ApplicationDeadline.Create(request.NewDeadlineUtc, request.AutoCloseEnabled);
        if (deadline.IsFailure) return Result.Failure<Guid>(deadline.Error);
        var renewed = _renewalService.Renew(posting, deadline.Value);
        if (renewed.IsFailure) return Result.Failure<Guid>(renewed.Error);
        var from = posting.Status;
        var archive = posting.Archive();
        if (archive.IsFailure) return Result.Failure<Guid>(archive.Error);
        await _postings.AddAsync(renewed.Value, cancellationToken);
        await _auditTrails.AddAsync(PostingAuditTrail.Create(renewed.Value.Id), cancellationToken);
        var sourceAudit = await _auditTrails.GetByPostingIdAsync(posting.Id, cancellationToken);
        sourceAudit?.RecordStatusChange(AuditActor.Create(AuditActorKind.Employer, request.UserId, "Employer").Value, StatusTransition.Create(from, posting.Status).Value, "renewed");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(renewed.Value.Id);
    }
}

public sealed class BulkRenewJobPostingsCommandHandler : ICommandHandler<BulkRenewJobPostingsCommand, IReadOnlyCollection<BulkRenewResultDto>>
{
    private readonly RenewJobPostingCommandHandler _renewHandler;

    public BulkRenewJobPostingsCommandHandler(RenewJobPostingCommandHandler renewHandler)
    {
        _renewHandler = renewHandler;
    }

    public async Task<Result<IReadOnlyCollection<BulkRenewResultDto>>> Handle(BulkRenewJobPostingsCommand request, CancellationToken cancellationToken)
    {
        var results = new List<BulkRenewResultDto>();
        foreach (var id in request.JobPostingIds.Distinct())
        {
            var result = await _renewHandler.Handle(new RenewJobPostingCommand(id, request.EmployerId, request.UserId, request.NewDeadlineUtc, request.AutoCloseEnabled), cancellationToken);
            results.Add(result.IsSuccess
                ? new BulkRenewResultDto(id, true, result.Value, null, null)
                : new BulkRenewResultDto(id, false, null, result.Error.Code, result.Error.Message));
        }

        return Result.Success<IReadOnlyCollection<BulkRenewResultDto>>(results);
    }
}

public sealed class ProcessExpiredPostingsCommandHandler : ICommandHandler<ProcessExpiredPostingsCommand, int>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly PostingExpirationPolicy _expirationPolicy;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public ProcessExpiredPostingsCommandHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, PostingExpirationPolicy expirationPolicy, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _expirationPolicy = expirationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(ProcessExpiredPostingsCommand request, CancellationToken cancellationToken)
    {
        var postings = await _postings.GetExpirableAsync(request.NowUtc, cancellationToken);
        var count = 0;
        foreach (var posting in postings.Where(p => _expirationPolicy.ShouldExpire(p, request.NowUtc)))
        {
            var from = posting.Status;
            var result = posting.Expire(request.NowUtc);
            if (result.IsFailure) continue;
            var audit = await _auditTrails.GetByPostingIdAsync(posting.Id, cancellationToken);
            audit?.RecordStatusChange(AuditActor.System(), StatusTransition.Create(from, posting.Status).Value, null, request.NowUtc);
            count++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(count);
    }
}
