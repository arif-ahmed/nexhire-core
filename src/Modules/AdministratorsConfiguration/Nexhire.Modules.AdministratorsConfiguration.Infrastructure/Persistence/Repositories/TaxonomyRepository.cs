using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Persistence.Repositories;

public sealed class TaxonomyRepository : ITaxonomyRepository
{
    private readonly AdministratorsConfigurationDbContext _context;

    public TaxonomyRepository(AdministratorsConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<Taxonomy?> GetByKindAsync(TaxonomyKind kind, CancellationToken cancellationToken)
    {
        return await _context.Taxonomies
            .Include(t => t.Terms) // Eagerly load owned collection
            .FirstOrDefaultAsync(t => t.Kind == kind, cancellationToken);
    }

    public async Task<TaxonomyTerm?> GetTermByCodeAsync(TermCode code, CancellationToken cancellationToken)
    {
        // Search across all taxonomies
        var taxonomies = await _context.Taxonomies
            .Include(t => t.Terms)
            .ToListAsync(cancellationToken);

        foreach (var taxonomy in taxonomies)
        {
            var term = taxonomy.Terms.FirstOrDefault(t => t.Code == code);
            if (term != null)
            {
                return term;
            }
        }

        return null;
    }

    public async Task<IReadOnlyCollection<TaxonomyTerm>> SearchTermsAsync(
        TaxonomyKind kind,
        string? labelLike,
        SkillCategory? category,
        TermStatus? status,
        CancellationToken cancellationToken)
    {
        var taxonomy = await GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Array.Empty<TaxonomyTerm>();

        var query = taxonomy.Terms.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(labelLike))
        {
            query = query.Where(t => t.Label.Contains(labelLike, StringComparison.OrdinalIgnoreCase));
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return query.ToList();
    }

    public async Task<IReadOnlyCollection<TaxonomyTerm>> GetTermsByUsageDescAsync(TaxonomyKind kind, CancellationToken cancellationToken)
    {
        var taxonomy = await GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return Array.Empty<TaxonomyTerm>();

        return taxonomy.Terms
            .OrderByDescending(t => t.UsageCount)
            .ToList();
    }

    public async Task AddAsync(Taxonomy taxonomy, CancellationToken cancellationToken)
    {
        await _context.Taxonomies.AddAsync(taxonomy, cancellationToken);
    }

    public void Update(Taxonomy taxonomy)
    {
        _context.Taxonomies.Update(taxonomy);
    }
}
