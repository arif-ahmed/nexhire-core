using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;

public sealed class ApiVersionRegistry : AggregateRoot<Guid>
{
    public static readonly Guid SingletonId = Guid.Parse("eb080000-0000-0000-0000-000000000001");
    private readonly List<ApiVersion> _versions = new();

    public IReadOnlyCollection<ApiVersion> Versions => _versions.AsReadOnly();

    private ApiVersionRegistry() : base(SingletonId) { }

    public static ApiVersionRegistry Create()
    {
        return new ApiVersionRegistry();
    }

    public Result RegisterVersion(string version, DateTime releasedOnUtc)
    {
        var apiVersionResult = ApiVersion.Create(version, ApiVersionStatus.Active, releasedOnUtc);
        if (apiVersionResult.IsFailure)
            return Result.Failure(apiVersionResult.Error);

        if (_versions.Any(v => v.Version == version))
            return Result.Failure(new Error("ApiVersion.Duplicate", $"Version '{version}' is already registered."));

        _versions.Add(apiVersionResult.Value);
        return Result.Success();
    }

    public Result DeprecateVersion(string version, DateTime sunsetOnUtc, string? migrationGuideUrl)
    {
        var apiVersion = _versions.FirstOrDefault(v => v.Version == version);
        if (apiVersion == null)
            return Result.Failure(new Error("ApiVersion.NotFound", $"Version '{version}' not found."));

        if (apiVersion.Status != ApiVersionStatus.Active)
            return Result.Failure(new Error("ApiVersion.NotActive", $"Version '{version}' is not in Active status."));

        if (sunsetOnUtc < DateTime.UtcNow.AddMonths(6))
            return Result.Failure(new Error("ApiVersion.InvalidSunset", "Sunset date must be at least 6 months in the future."));

        // Update the version in the collection
        _versions.Remove(apiVersion);
        var deprecatedVersion = apiVersion.Deprecate(sunsetOnUtc, migrationGuideUrl);
        _versions.Add(deprecatedVersion);

        return Result.Success();
    }

    public Result SunsetVersion(string version)
    {
        var apiVersion = _versions.FirstOrDefault(v => v.Version == version);
        if (apiVersion == null)
            return Result.Failure(new Error("ApiVersion.NotFound", $"Version '{version}' not found."));

        if (apiVersion.Status != ApiVersionStatus.Deprecated)
            return Result.Failure(new Error("ApiVersion.NotDeprecated", $"Version '{version}' must be deprecated before it can be sunset."));

        if (DateTime.UtcNow < apiVersion.SunsetOnUtc)
            return Result.Failure(new Error("ApiVersion.BeforeSunsetDate", $"Cannot sunset version '{version}' before the announced sunset date."));

        // Enforce invariant: At least 2 MAJOR versions remain non-Sunset at any time (minimum support window)
        var nonSunsetCount = _versions.Count(v => v.Status != ApiVersionStatus.Sunset);
        if (nonSunsetCount <= 2)
            return Result.Failure(new Error("ApiVersion.MinVersionsRequired", "At least 2 active or deprecated major API versions must be supported."));

        _versions.Remove(apiVersion);
        var sunsetVersion = apiVersion.Sunset();
        _versions.Add(sunsetVersion);

        return Result.Success();
    }
}
