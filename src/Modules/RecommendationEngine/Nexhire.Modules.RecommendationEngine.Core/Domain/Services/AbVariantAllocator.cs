using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class AbVariantAllocator
{
    public MatchingWeightProfile AllocateVariant(Guid seekerId, List<MatchingWeightProfile> activeProfiles)
    {
        if (activeProfiles == null || activeProfiles.Count == 0)
        {
            return MatchingWeightProfile.CreateInitial();
        }

        if (activeProfiles.Count == 1)
        {
            return activeProfiles[0];
        }

        // Compute stable deterministic hash (0 to 99) from Guid
        var bytes = seekerId.ToByteArray();
        int hash = 17;
        foreach (var b in bytes)
        {
            hash = (hash * 31) + b;
        }
        int bucket = Math.Abs(hash) % 100;

        // Stratified allocation based on variant allocation percent
        // Sort by VariantId to ensure stable ordering
        var sortedProfiles = activeProfiles.OrderBy(p => p.VariantId).ToList();

        int cumulativeLowerBound = 0;
        foreach (var profile in sortedProfiles)
        {
            int cumulativeUpperBound = cumulativeLowerBound + profile.VariantAllocationPercent;
            if (bucket >= cumulativeLowerBound && bucket < cumulativeUpperBound)
            {
                return profile;
            }
            cumulativeLowerBound = cumulativeUpperBound;
        }

        // Fallback to the first one or control if anything is out of bounds
        return sortedProfiles.FirstOrDefault(p => p.VariantId.Equals("control", StringComparison.OrdinalIgnoreCase)) 
               ?? sortedProfiles[0];
    }
}
