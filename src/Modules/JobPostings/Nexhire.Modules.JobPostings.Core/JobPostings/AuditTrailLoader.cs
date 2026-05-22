using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;

namespace Nexhire.Modules.JobPostings.Core.JobPostings;

internal static class AuditTrailLoader
{
    public static async Task<PostingAuditTrail> LoadAsync(Guid postingId, IPostingAuditTrailRepository auditTrails, CancellationToken cancellationToken)
    {
        var trail = await auditTrails.GetByPostingIdAsync(postingId, cancellationToken);
        if (trail is not null)
        {
            return trail;
        }

        trail = PostingAuditTrail.Create(postingId);
        await auditTrails.AddAsync(trail, cancellationToken);
        return trail;
    }
}
