using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Services;

public sealed class ContextHelpResolver
{
    public IReadOnlyList<Guid> Resolve(
        string contextKey,
        string viewerRole,
        Language language,
        IReadOnlyList<FaqEntry> candidates)
    {
        var role = Enum.TryParse<VisibleRole>(viewerRole, true, out var r) ? r : (VisibleRole?)null;

        return candidates
            .Where(e => e.Status == ContentStatus.Published)
            .Where(e => e.ContextKeys.Contains(contextKey))
            .Where(e => e.VisibleRoles is not null &&
                        (e.VisibleRoles.Contains(VisibleRole.All) ||
                         (role.HasValue && e.VisibleRoles.Contains(role.Value))))
            .Where(e => e.Localizations.ContainsKey(language))
            .OrderByDescending(e => e.Kind == FaqEntryKind.HelpArticle ? 1 : 0)
            .ThenByDescending(e => e.UpdatedOnUtc)
            .Select(e => e.Id)
            .ToList();
    }
}
