using System;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.Services;

public static class CandidateSnapshotBuilder
{
    public static Result<CandidateSnapshot> Build(
        JobSeekerProfileSnapshotDto profile,
        SnapshotOverrides? overrides)
    {
        var fullName = !string.IsNullOrWhiteSpace(overrides?.FullName) ? overrides.FullName : profile.FullName;
        var email = !string.IsNullOrWhiteSpace(overrides?.Email) ? overrides.Email : profile.Email;
        var mobile = !string.IsNullOrWhiteSpace(overrides?.Mobile) ? overrides.Mobile : profile.Mobile;
        var currentLocation = overrides?.CurrentLocation ?? profile.CurrentLocation;

        return CandidateSnapshot.Create(
            fullName,
            email,
            mobile,
            currentLocation,
            profile.EducationSummary,
            profile.ExperienceSummary,
            profile.Skills,
            profile.IsLevel2Complete,
            DateTime.UtcNow);
    }
}
