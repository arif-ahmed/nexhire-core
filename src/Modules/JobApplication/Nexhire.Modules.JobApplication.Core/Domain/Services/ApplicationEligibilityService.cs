using System;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.Services;

public static class ApplicationEligibilityService
{
    public static Result CheckCanApply(
        Guid jobSeekerId,
        Guid jobPostingId,
        PostingApplicabilitySnapshot posting,
        bool isLevel2Complete,
        Application? existingNonTerminalApplication)
    {
        if (existingNonTerminalApplication != null)
        {
            return Result.Failure(new Error("E-APP-DUPLICATE", $"An active application already exists for seeker '{jobSeekerId}' and posting '{jobPostingId}' (ID: {existingNonTerminalApplication.Id.Value})."));
        }

        if (!isLevel2Complete)
        {
            return Result.Failure(new Error("E-APP-PROFILE-INCOMPLETE", "Job seeker profile Level 2 must be complete."));
        }

        var isStatusActive = string.Equals(posting.Status, "Active", StringComparison.OrdinalIgnoreCase);
        var isDeadlinePassed = posting.DeadlineUtc.HasValue && posting.DeadlineUtc.Value < DateTime.UtcNow;

        if (!isStatusActive || isDeadlinePassed)
        {
            return Result.Failure(new Error("E-APP-POSTING-CLOSED", "The job posting is closed or inactive."));
        }

        return Result.Success();
    }
}
