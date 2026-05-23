using System.Text.Json;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;

public enum MappingDirection { Inbound, Outbound, Bidirectional }
public enum TransformKind { Direct, SalaryRange, LocationNormalise, SkillTaxonomyMap, DateParse, Constant }

public sealed class MappingProfile : AggregateRoot<Guid>
{
    private readonly List<FieldMapping> _fieldMappings = new();

    public string PortalName { get; private set; } = null!;
    public string SchemaVersion { get; private set; } = null!;
    public MappingDirection Direction { get; private set; }
    public IReadOnlyCollection<FieldMapping> FieldMappings => _fieldMappings.AsReadOnly();
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private MappingProfile() { }

    private MappingProfile(Guid id, string portalName, string schemaVersion, MappingDirection direction, DateTime createdOnUtc) : base(id)
    {
        PortalName = portalName;
        SchemaVersion = schemaVersion;
        Direction = direction;
        IsActive = false;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = createdOnUtc;
    }

    public static Result<MappingProfile> Create(string portalName, string schemaVersion, MappingDirection direction)
    {
        if (string.IsNullOrWhiteSpace(portalName))
            return Result.Failure<MappingProfile>(new Error("MappingProfile.PortalNameRequired", "Portal name is required."));
        if (string.IsNullOrWhiteSpace(schemaVersion))
            return Result.Failure<MappingProfile>(new Error("MappingProfile.SchemaVersionRequired", "Schema version is required."));

        return Result.Success(new MappingProfile(Guid.NewGuid(), portalName.Trim(), schemaVersion.Trim(), direction, DateTime.UtcNow));
    }

    public Result AddFieldMapping(Guid mappingId, string sourcePath, string targetPath, TransformKind transformKind, string? transformArgs, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return Result.Failure(new Error("MappingProfile.SourcePathRequired", "Source path is required."));
        if (string.IsNullOrWhiteSpace(targetPath))
            return Result.Failure(new Error("MappingProfile.TargetPathRequired", "Target path is required."));

        if (_fieldMappings.Any(m => m.SourcePath == sourcePath))
            return Result.Failure(new Error("MappingProfile.DuplicateSourcePath", $"A field mapping for source path '{sourcePath}' already exists."));

        // Validate JSON if transformArgs is present
        if (!string.IsNullOrWhiteSpace(transformArgs))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(transformArgs);
            }
            catch (JsonException)
            {
                return Result.Failure(new Error("MappingProfile.InvalidJsonArgs", "Transform arguments must be a valid JSON string."));
            }
        }

        var mapping = new FieldMapping(mappingId, sourcePath.Trim(), targetPath.Trim(), transformKind, transformArgs?.Trim(), isRequired);
        _fieldMappings.Add(mapping);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveFieldMapping(Guid mappingId)
    {
        var mapping = _fieldMappings.FirstOrDefault(m => m.Id == mappingId);
        if (mapping == null)
            return Result.Failure(new Error("MappingProfile.MappingNotFound", "Field mapping not found."));

        _fieldMappings.Remove(mapping);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

public sealed class FieldMapping : Entity<Guid>
{
    public string SourcePath { get; private set; } = null!;
    public string TargetPath { get; private set; } = null!;
    public TransformKind TransformKind { get; private set; }
    public string? TransformArgs { get; private set; }
    public bool IsRequired { get; private set; }

    private FieldMapping() { }

    internal FieldMapping(Guid id, string sourcePath, string targetPath, TransformKind transformKind, string? transformArgs, bool isRequired) : base(id)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath;
        TransformKind = transformKind;
        TransformArgs = transformArgs;
        IsRequired = isRequired;
    }
}
