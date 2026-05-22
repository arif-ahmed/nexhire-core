using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class JobIndexEntryRepository : IJobIndexEntryRepository
{
    private readonly SearchDiscoveryDbContext _db;

    public JobIndexEntryRepository(SearchDiscoveryDbContext db)
    {
        _db = db;
    }

    public async Task<JobIndexEntry?> GetByIdAsync(Guid postingId, CancellationToken ct = default)
        => await _db.JobIndexEntries.FindAsync([postingId], ct);

    public async Task<bool> ExistsAsync(Guid postingId, CancellationToken ct = default)
        => await _db.JobIndexEntries.AnyAsync(e => e.Id == postingId, ct);

    public async Task<IReadOnlyList<JobIndexEntry>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        var query = _db.JobIndexEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = $"%{criteria.Keyword.Trim().ToLower()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.Title, keyword) ||
                EF.Functions.ILike(e.Summary, keyword));
        }

        var filters = criteria.Filters;
        if (filters.EmploymentTypes.Count > 0)
            query = query.Where(e => filters.EmploymentTypes.Contains(e.EmploymentType));

        if (filters.WorkFormats.Count > 0)
            query = query.Where(e => filters.WorkFormats.Contains(e.WorkFormat));

        if (filters.Location is not null)
            query = query.Where(e => e.Location.District == filters.Location.District);

        if (filters.SalaryMin.HasValue)
            query = query.Where(e => e.SalaryMax >= filters.SalaryMin);

        if (filters.SalaryMax.HasValue)
            query = query.Where(e => e.SalaryMin <= filters.SalaryMax);

        if (filters.SectorIndustry is not null)
            query = query.Where(e => e.SectorIndustry == filters.SectorIndustry);

        if (filters.DeadlineBefore.HasValue)
            query = query.Where(e => e.ApplicationDeadlineUtc <= filters.DeadlineBefore);

        return await query
            .OrderByDescending(e => e.PostedOnUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        var query = _db.JobIndexEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = $"%{criteria.Keyword.Trim().ToLower()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.Title, keyword) ||
                EF.Functions.ILike(e.Summary, keyword));
        }

        var filters = criteria.Filters;
        if (filters.EmploymentTypes.Count > 0)
            query = query.Where(e => filters.EmploymentTypes.Contains(e.EmploymentType));
        if (filters.WorkFormats.Count > 0)
            query = query.Where(e => filters.WorkFormats.Contains(e.WorkFormat));
        if (filters.Location is not null)
            query = query.Where(e => e.Location.District == filters.Location.District);
        if (filters.SalaryMin.HasValue)
            query = query.Where(e => e.SalaryMax >= filters.SalaryMin);
        if (filters.SalaryMax.HasValue)
            query = query.Where(e => e.SalaryMin <= filters.SalaryMax);

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(JobIndexEntry entry, CancellationToken ct = default)
        => await _db.JobIndexEntries.AddAsync(entry, ct);

    public async Task UpdateAsync(JobIndexEntry entry, CancellationToken ct = default)
    {
        _db.JobIndexEntries.Update(entry);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid postingId, CancellationToken ct = default)
    {
        var entry = await _db.JobIndexEntries.FindAsync([postingId], ct);
        if (entry is not null)
            _db.JobIndexEntries.Remove(entry);
    }
}
