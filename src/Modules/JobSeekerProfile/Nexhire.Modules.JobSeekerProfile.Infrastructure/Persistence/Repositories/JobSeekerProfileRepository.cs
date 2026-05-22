using Microsoft.EntityFrameworkCore;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Repositories;

public class JobSeekerProfileRepository : IJobSeekerProfileRepository
{
    private readonly JobSeekerProfileDbContext _dbContext;

    public JobSeekerProfileRepository(JobSeekerProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Aggregates.JobSeekerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobSeekerProfiles
            .Include(ep => ep.Education)
            .Include(ep => ep.Experience)
            .Include(ep => ep.Skills)
            .Include(ep => ep.Documents)
            .FirstOrDefaultAsync(ep => ep.Id == id, cancellationToken);
    }

    public async Task<Aggregates.JobSeekerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobSeekerProfiles
            .Include(ep => ep.Education)
            .Include(ep => ep.Experience)
            .Include(ep => ep.Skills)
            .Include(ep => ep.Documents)
            .FirstOrDefaultAsync(ep => ep.UserId == userId, cancellationToken);
    }

    public async Task<Aggregates.JobSeekerProfile?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        
        var escapedSlug = slug.Replace("\"", "\\\"");
        
        // Since PublicSharing is serialized to jsonb, we query using a string lookup for the slug property
        return await _dbContext.JobSeekerProfiles
            .Include(ep => ep.Education)
            .Include(ep => ep.Experience)
            .Include(ep => ep.Skills)
            .Include(ep => ep.Documents)
            .FirstOrDefaultAsync(x => EF.Functions.Like(EF.Property<string>(x, "PublicSharing"), $"%\"slug\":\"{escapedSlug}\"%"), cancellationToken);
    }

    public async Task<bool> IsSlugTakenAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        
        var escapedSlug = slug.Replace("\"", "\\\"");
        
        return await _dbContext.JobSeekerProfiles
            .AnyAsync(x => EF.Functions.Like(EF.Property<string>(x, "PublicSharing"), $"%\"slug\":\"{escapedSlug}\"%"), cancellationToken);
    }

    public async Task AddAsync(Aggregates.JobSeekerProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.JobSeekerProfiles.AddAsync(profile, cancellationToken);
    }

    public Task UpdateAsync(Aggregates.JobSeekerProfile profile, CancellationToken cancellationToken = default)
    {
        _dbContext.JobSeekerProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
