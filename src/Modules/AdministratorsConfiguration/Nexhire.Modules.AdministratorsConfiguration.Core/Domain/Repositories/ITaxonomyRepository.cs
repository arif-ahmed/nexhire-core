using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;

public interface ITaxonomyRepository
{
    Task<Taxonomy?> GetByKindAsync(TaxonomyKind kind, CancellationToken cancellationToken);
    Task<TaxonomyTerm?> GetTermByCodeAsync(TermCode code, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaxonomyTerm>> SearchTermsAsync(
        TaxonomyKind kind,
        string? labelLike,
        SkillCategory? category,
        TermStatus? status,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaxonomyTerm>> GetTermsByUsageDescAsync(TaxonomyKind kind, CancellationToken cancellationToken);
    Task AddAsync(Taxonomy taxonomy, CancellationToken cancellationToken);
    void Update(Taxonomy taxonomy);
}

public interface IAdministratorsConfigurationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
