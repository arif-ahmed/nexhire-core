using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.JobPostings;

public static class JobPostingMappers
{
    public static JobPostingSummaryDto ToSummaryDto(JobPosting posting) =>
        new(posting.Id, posting.Title.Value, posting.Status.ToString(), posting.Deadline.DateUtc, posting.Visibility.Level.ToString(), posting.UpdatedOnUtc);

    public static AdminJobPostingListItemDto ToAdminListItemDto(JobPosting posting) =>
        new(
            posting.Id,
            posting.EmployerId,
            posting.Title.Value,
            posting.Status.ToString(),
            posting.Location is null ? string.Empty : $"{posting.Location.City}, {posting.Location.District}, {posting.Location.Country}",
            posting.Deadline.DateUtc,
            posting.CreatedOnUtc,
            posting.UpdatedOnUtc);

    public static JobPostingDto ToDto(JobPosting posting) =>
        new(
            posting.Id,
            posting.EmployerId,
            posting.Title.Value,
            posting.Summary.Value,
            posting.Status.ToString(),
            posting.ContractType.ToString(),
            posting.EducationLevel.ToString(),
            posting.WorkFormat.ToString(),
            posting.Deadline.DateUtc,
            posting.Visibility.Level.ToString(),
            posting.RequiredSkills.Select(x => x.CanonicalRef.TaxonomyCode).ToArray(),
            posting.CreatedOnUtc,
            posting.UpdatedOnUtc,
            posting.PublishedOnUtc,
            posting.RenewedFromPostingId);

    public static SchemaOrgJobPostingDto ToDto(SchemaOrgJobPosting schemaOrg) =>
        new(schemaOrg.Properties, schemaOrg.IsCompliant, schemaOrg.Violations);

    public static AuditEntryDto ToDto(AuditEntry entry) =>
        new(
            entry.Kind.ToString(),
            entry.Actor.Kind.ToString(),
            entry.Actor.UserId,
            entry.Actor.DisplayName,
            entry.StatusTransition?.From.ToString(),
            entry.StatusTransition?.To.ToString(),
            entry.ChangedFields,
            entry.Reason,
            entry.OccurredOnUtc);

    public static Result<PostingVisibility> ToVisibility(PostingVisibilityDto dto)
    {
        TargetingCriteria? criteria = null;
        if (dto.TargetingCriteria is not null)
        {
            var criteriaResult = TargetingCriteria.Create(dto.TargetingCriteria.SkillCodes, dto.TargetingCriteria.Locations, dto.TargetingCriteria.SeekerGroupIds);
            if (criteriaResult.IsFailure) return Result.Failure<PostingVisibility>(criteriaResult.Error);
            criteria = criteriaResult.Value;
        }

        return PostingVisibility.Create(dto.Level, criteria);
    }
}
