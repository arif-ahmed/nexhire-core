using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class MatchThresholdConfiguration : AggregateRoot<MatchThresholdConfigurationId>
{
    private readonly List<ThresholdChangeEntry> _changeLog = new();

    public int GlobalThresholdPercent { get; private set; }
    public IReadOnlyCollection<ThresholdChangeEntry> ChangeLog => _changeLog.AsReadOnly();

    private MatchThresholdConfiguration() : base(MatchThresholdConfigurationId.New()) { }

    private MatchThresholdConfiguration(MatchThresholdConfigurationId id, int globalThresholdPercent) : base(id)
    {
        GlobalThresholdPercent = globalThresholdPercent;
    }

    public static MatchThresholdConfiguration CreateDefault()
    {
        return new MatchThresholdConfiguration(MatchThresholdConfigurationId.New(), 60);
    }

    public Result UpdateGlobalThreshold(int newThreshold, Guid adminId)
    {
        if (newThreshold is < 0 or > 100)
        {
            return Result.Failure(new Error("E-THRESHOLD-OUT-OF-RANGE", "Match threshold percentage must be between 0 and 100."));
        }

        var entry = new ThresholdChangeEntry(GlobalThresholdPercent, newThreshold, adminId, "Global", DateTime.UtcNow);
        _changeLog.Add(entry);

        GlobalThresholdPercent = newThreshold;

        return Result.Success();
    }
}

public sealed record ThresholdChangeEntry(
    int OldValue,
    int NewValue,
    Guid ChangedBy,
    string Scope,
    DateTime ChangedAtUtc);
