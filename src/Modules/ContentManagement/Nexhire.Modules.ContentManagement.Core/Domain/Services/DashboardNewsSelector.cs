using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Services;

public record ArticleSummary(Guid ArticleId, Guid PrimaryCategoryId, DateTime PublishedOnUtc, Language Language);

public sealed class DashboardNewsSelector
{
    public IReadOnlyList<Guid> Select(
        ContentPreference preference,
        SeekerPersonalizationAttributes? profileAttributes,
        IReadOnlyList<ArticleSummary> candidates,
        int maxItems)
    {
        IEnumerable<ArticleSummary> filtered;

        if (preference.IncludedCategoryIds.Count > 0)
        {
            var included = preference.IncludedCategoryIds.ToHashSet();
            filtered = candidates.Where(a => included.Contains(a.PrimaryCategoryId));
        }
        else if (profileAttributes is not null)
        {
            filtered = candidates; // Attribute matching is done by the caller (repository-level tag/sector match)
        }
        else
        {
            filtered = candidates; // Default fallback: newest global
        }

        var hidden = preference.HiddenCategoryIds.ToHashSet();
        filtered = filtered.Where(a => !hidden.Contains(a.PrimaryCategoryId));

        var preferred = filtered
            .OrderByDescending(a => a.PublishedOnUtc)
            .Take(maxItems)
            .ToList();

        return preferred.Select(a => a.ArticleId).ToList();
    }
}
