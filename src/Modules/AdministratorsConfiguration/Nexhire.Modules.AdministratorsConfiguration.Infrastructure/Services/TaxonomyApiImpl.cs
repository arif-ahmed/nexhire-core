using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts;
using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.DTOs;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Repositories;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Services;

public sealed class TaxonomyApiImpl : ITaxonomyApi, INotificationHandler<TaxonomyUpdatedDomainEvent>
{
    private readonly ITaxonomyRepository _repository;
    private readonly IMemoryCache _cache;

    private static readonly string TermsMapCacheKey = "Taxonomy_Terms_Map";
    private static readonly string ActiveSkillsCacheKey = "Taxonomy_Active_Skills";
    private static readonly string SkillLabelsMapCacheKey = "Taxonomy_Skill_Labels";
    private static readonly string ActiveCodesPrefixCacheKey = "Taxonomy_Active_Codes_";
    private static readonly string TaxonomyVersionsCacheKey = "Taxonomy_Versions";

    public TaxonomyApiImpl(ITaxonomyRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<CanonicalSkillRef>> MapSkillAsync(string rawSkillLabel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawSkillLabel))
        {
            return Result.Failure<CanonicalSkillRef>(new Error("MapSkill.EmptyLabel", "Skill label cannot be empty."));
        }

        var normalizedSearch = rawSkillLabel.Trim().ToLowerInvariant();

        var labelsMap = await _cache.GetOrCreateAsync(SkillLabelsMapCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return await LoadSkillLabelsMapAsync(cancellationToken);
        });

        if (labelsMap != null && labelsMap.TryGetValue(normalizedSearch, out var canonicalRef))
        {
            return Result.Success(canonicalRef);
        }

        // Fuzzy fallback match (if exact normalized match fails, search for substring match)
        if (labelsMap != null)
        {
            var fuzzyMatch = labelsMap.FirstOrDefault(x => x.Key.Contains(normalizedSearch) || normalizedSearch.Contains(x.Key));
            if (fuzzyMatch.Value != null)
            {
                return Result.Success(fuzzyMatch.Value);
            }
        }

        return Result.Failure<CanonicalSkillRef>(new Error("E-TAXO-NO-MATCH", $"No matching canonical skill found for '{rawSkillLabel}'."));
    }

    public async Task<bool> IsValidSkillCodeAsync(string taxonomyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taxonomyCode)) return false;

        var activeSkills = await _cache.GetOrCreateAsync(ActiveSkillsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return await LoadActiveCodesAsync(TaxonomyKind.Skills, cancellationToken);
        });

        return activeSkills != null && activeSkills.Contains(taxonomyCode.Trim().ToUpperInvariant());
    }

    public async Task<TaxonomyTermDto?> GetTermAsync(string taxonomyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taxonomyCode)) return null;

        var termsMap = await _cache.GetOrCreateAsync(TermsMapCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return await LoadAllTermsMapAsync(cancellationToken);
        });

        if (termsMap != null && termsMap.TryGetValue(taxonomyCode.Trim().ToUpperInvariant(), out var termDto))
        {
            return termDto;
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, bool>> AreValidCodesAsync(IEnumerable<string> taxonomyCodes, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, bool>();
        if (taxonomyCodes == null) return result;

        var termsMap = await _cache.GetOrCreateAsync(TermsMapCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return await LoadAllTermsMapAsync(cancellationToken);
        });

        foreach (var rawCode in taxonomyCodes)
        {
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                result[rawCode] = false;
                continue;
            }

            var code = rawCode.Trim().ToUpperInvariant();
            if (termsMap != null && termsMap.TryGetValue(code, out var term))
            {
                result[rawCode] = term.Status == "Active";
            }
            else
            {
                result[rawCode] = false;
            }
        }

        return result;
    }

    public async Task<int> GetTaxonomyVersionAsync(string kind, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<TaxonomyKind>(kind, true, out var taxonomyKind))
        {
            return 0;
        }

        var versions = await _cache.GetOrCreateAsync(TaxonomyVersionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            var map = new Dictionary<TaxonomyKind, int>();
            foreach (TaxonomyKind k in Enum.GetValues<TaxonomyKind>())
            {
                var tax = await _repository.GetByKindAsync(k, cancellationToken);
                map[k] = tax?.Version ?? 1;
            }
            return map;
        });

        if (versions != null && versions.TryGetValue(taxonomyKind, out var version))
        {
            return version;
        }

        return 1;
    }

    // Invalidation Handler:
    // Listens to the in-process TaxonomyUpdatedDomainEvent and invalidates cache keys
    public Task Handle(TaxonomyUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _cache.Remove(TermsMapCacheKey);
        _cache.Remove(ActiveSkillsCacheKey);
        _cache.Remove(SkillLabelsMapCacheKey);
        _cache.Remove(TaxonomyVersionsCacheKey);
        _cache.Remove($"{ActiveCodesPrefixCacheKey}{TaxonomyKind.Skills}");
        _cache.Remove($"{ActiveCodesPrefixCacheKey}{TaxonomyKind.Occupations}");
        _cache.Remove($"{ActiveCodesPrefixCacheKey}{TaxonomyKind.TrainingPrograms}");

        return Task.CompletedTask;
    }

    private async Task<HashSet<string>> LoadActiveCodesAsync(TaxonomyKind kind, CancellationToken cancellationToken)
    {
        var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
        if (taxonomy == null) return new HashSet<string>();

        return taxonomy.Terms
            .Where(t => t.Status == TermStatus.Active)
            .Select(t => t.Code.Value)
            .ToHashSet();
    }

    private async Task<Dictionary<string, CanonicalSkillRef>> LoadSkillLabelsMapAsync(CancellationToken cancellationToken)
    {
        var taxonomy = await _repository.GetByKindAsync(TaxonomyKind.Skills, cancellationToken);
        if (taxonomy == null) return new Dictionary<string, CanonicalSkillRef>();

        return taxonomy.Terms
            .Where(t => t.Status == TermStatus.Active)
            .ToDictionary(
                t => t.Label.Trim().ToLowerInvariant(),
                t => new CanonicalSkillRef(t.Code.Value, t.Label)
            );
    }

    private async Task<Dictionary<string, TaxonomyTermDto>> LoadAllTermsMapAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, TaxonomyTermDto>();

        foreach (TaxonomyKind kind in Enum.GetValues<TaxonomyKind>())
        {
            var taxonomy = await _repository.GetByKindAsync(kind, cancellationToken);
            if (taxonomy == null) continue;

            foreach (var term in taxonomy.Terms)
            {
                map[term.Code.Value] = new TaxonomyTermDto(
                    term.Code.Value,
                    kind.ToString(),
                    term.Label,
                    term.Category?.ToString(),
                    term.ParentCode?.Value,
                    term.Status.ToString(),
                    term.ReplacedByCode?.Value,
                    term.UsageCount
                );
            }
        }

        return map;
    }
}
