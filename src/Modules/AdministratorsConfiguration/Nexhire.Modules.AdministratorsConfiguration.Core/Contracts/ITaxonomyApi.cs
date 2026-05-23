using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Contracts;

public interface ITaxonomyApi
{
    /// <summary>
    /// Maps a raw, free-text skill label to a canonical skill.
    /// Performs fuzzy/normalized match against active terms in the Skills taxonomy.
    /// </summary>
    Task<Result<CanonicalSkillRef>> MapSkillAsync(string rawSkillLabel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cheap validity check for a skill code.
    /// Returns true only if the code exists in the Skills taxonomy AND is Active.
    /// </summary>
    Task<bool> IsValidSkillCodeAsync(string taxonomyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the canonical term (any kind) for a code.
    /// Returns null if the code does not exist.
    /// </summary>
    Task<TaxonomyTermDto?> GetTermAsync(string taxonomyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk validity check. Returns a mapping of each input code to whether it is a valid Active code.
    /// </summary>
    Task<IReadOnlyDictionary<string, bool>> AreValidCodesAsync(IEnumerable<string> taxonomyCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a taxonomy kind.
    /// </summary>
    Task<int> GetTaxonomyVersionAsync(string kind, CancellationToken cancellationToken = default);
}
