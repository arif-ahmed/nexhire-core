using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;

public static class ResumeReplacementService
{
    public static Result Replace(Resume? existing, Resume incoming)
    {
        if (incoming == null)
        {
            return Result.Failure(new Error("ResumeReplacement.NullIncoming", "Incoming resume cannot be null."));
        }

        if (existing != null)
        {
            if (existing.ProfileId != incoming.ProfileId)
            {
                return Result.Failure(new Error("ResumeReplacement.ProfileMismatch", "Existing and incoming resumes must belong to the same profile."));
            }

            existing.Supersede();
        }

        return Result.Success();
    }
}
