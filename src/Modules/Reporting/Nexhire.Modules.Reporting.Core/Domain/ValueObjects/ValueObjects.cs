using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using System.Text.RegularExpressions;

namespace Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    public string Value { get; }
    private EmailAddress(string value) => Value = value;
    public static Result<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<EmailAddress>(new Error("Email.Empty", "Email cannot be empty."));
        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return Result.Failure<EmailAddress>(new Error("Email.Invalid", "Email is not valid."));
        return Result.Success(new EmailAddress(value.ToLowerInvariant()));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed class FileReference : ValueObject
{
    public string StorageKey { get; }
    public string OriginalFileName { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }
    private FileReference(string storageKey, string originalFileName, string mimeType, long sizeBytes)
    { StorageKey = storageKey; OriginalFileName = originalFileName; MimeType = mimeType; SizeBytes = sizeBytes; }
    public static Result<FileReference> Create(string storageKey, string originalFileName, string mimeType, long sizeBytes)
    {
        if (sizeBytes <= 0) return Result.Failure<FileReference>(new Error("FileReference.InvalidSize", "File size must be positive."));
        if (string.IsNullOrWhiteSpace(storageKey)) return Result.Failure<FileReference>(new Error("FileReference.EmptyKey", "Storage key required."));
        return Result.Success(new FileReference(storageKey, originalFileName, mimeType, sizeBytes));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return StorageKey; }
}

public sealed class DateRange : ValueObject
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
    private DateRange(DateTime start, DateTime end) { StartUtc = start; EndUtc = end; }
    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (start > end) return Result.Failure<DateRange>(new Error("DateRange.Invalid", "Start must not be after end."));
        return Result.Success(new DateRange(start, end));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return StartUtc; yield return EndUtc; }
}

public sealed class AlertCondition : ValueObject
{
    public Comparator Comparator { get; }
    public decimal Threshold { get; }
    private AlertCondition(Comparator comparator, decimal threshold) { Comparator = comparator; Threshold = threshold; }
    public static Result<AlertCondition> Create(Comparator comparator, decimal threshold)
        => Result.Success(new AlertCondition(comparator, threshold));
    public bool IsBreach(decimal observed) => Comparator == Comparator.GreaterThan ? observed > Threshold : observed < Threshold;
    public override IEnumerable<object> GetEqualityComponents() { yield return Comparator; yield return Threshold; }
}

public sealed class ReportFilter : ValueObject
{
    public string Field { get; }
    public FilterOperator Operator { get; }
    public IReadOnlyList<string> Values { get; }
    private ReportFilter(string field, FilterOperator op, List<string> values)
    { Field = field; Operator = op; Values = values.AsReadOnly(); }
    public static Result<ReportFilter> Create(string field, FilterOperator op, List<string> values)
    {
        if (string.IsNullOrWhiteSpace(field)) return Result.Failure<ReportFilter>(new Error("ReportFilter.EmptyField", "Field required."));
        if (values.Count == 0) return Result.Failure<ReportFilter>(new Error("ReportFilter.EmptyValues", "Values required."));
        if (op == FilterOperator.Between && values.Count != 2) return Result.Failure<ReportFilter>(new Error("ReportFilter.BetweenValues", "Between requires exactly 2 values."));
        return Result.Success(new ReportFilter(field, op, values));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Field; yield return Operator; foreach (var v in Values) yield return v; }
}

public sealed class ReportSpec : ValueObject
{
    public IReadOnlyList<string> Metrics { get; }
    public IReadOnlyList<string> Dimensions { get; }
    public IReadOnlyList<ReportFilter> Filters { get; }
    public VisualizationType Visualization { get; }
    private ReportSpec(List<string> metrics, List<string> dimensions, List<ReportFilter> filters, VisualizationType vis)
    { Metrics = metrics.AsReadOnly(); Dimensions = dimensions.AsReadOnly(); Filters = filters.AsReadOnly(); Visualization = vis; }
    public static Result<ReportSpec> Create(List<string> metrics, List<string> dimensions, List<ReportFilter> filters, VisualizationType visualization, Func<string, bool> isKnownMetric, Func<string, bool> isKnownDimension)
    {
        if (metrics.Count == 0) return Result.Failure<ReportSpec>(new Error("ReportSpec.NoMetrics", "At least one metric required."));
        foreach (var m in metrics)
            if (!isKnownMetric(m)) return Result.Failure<ReportSpec>(new Error("ReportSpec.UnknownMetric", $"Unknown metric: {m}"));
        foreach (var d in dimensions)
            if (!isKnownDimension(d)) return Result.Failure<ReportSpec>(new Error("ReportSpec.UnknownDimension", $"Unknown dimension: {d}"));
        return Result.Success(new ReportSpec(metrics, dimensions, filters, visualization));
    }
    public static ReportSpec CreateUnsafe(List<string> metrics, List<string> dimensions, List<ReportFilter> filters, VisualizationType visualization)
        => new(metrics, dimensions, filters, visualization);
    public override IEnumerable<object> GetEqualityComponents() { yield return Visualization; foreach (var m in Metrics) yield return m; }
}

public sealed class ConfigurableParameter : ValueObject
{
    public string Name { get; }
    public ParameterKind Kind { get; }
    public bool Required { get; }
    public string? DefaultValue { get; }
    private ConfigurableParameter(string name, ParameterKind kind, bool required, string? defaultValue)
    { Name = name; Kind = kind; Required = required; DefaultValue = defaultValue; }
    public static Result<ConfigurableParameter> Create(string name, ParameterKind kind, bool required, string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<ConfigurableParameter>(new Error("ConfigurableParameter.EmptyName", "Name required."));
        return Result.Success(new ConfigurableParameter(name, kind, required, defaultValue));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Name; yield return Kind; yield return Required; }
}

public sealed class ResolvedParameters : ValueObject
{
    public IReadOnlyDictionary<string, string> Values { get; }
    private ResolvedParameters(Dictionary<string, string> values) => Values = values;
    public static Result<ResolvedParameters> Create(Dictionary<string, string> values, IEnumerable<ConfigurableParameter> configurableParameters)
    {
        foreach (var p in configurableParameters.Where(p => p.Required))
            if (!values.ContainsKey(p.Name))
                return Result.Failure<ResolvedParameters>(new Error("ResolvedParameters.Missing", $"Required parameter missing: {p.Name}"));
        return Result.Success(new ResolvedParameters(values));
    }
    public static ResolvedParameters Empty() => new(new Dictionary<string, string>());
    public override IEnumerable<object> GetEqualityComponents() { foreach (var kv in Values) { yield return kv.Key; yield return kv.Value; } }
}

public sealed class ReportVisibility : ValueObject
{
    public IReadOnlySet<string> AllowedRoles { get; }
    private ReportVisibility(HashSet<string> roles) => AllowedRoles = roles;
    public static Result<ReportVisibility> Create(HashSet<string> roles)
    {
        if (roles.Count == 0) return Result.Failure<ReportVisibility>(new Error("ReportVisibility.Empty", "At least one role required."));
        return Result.Success(new ReportVisibility(roles));
    }
    public bool AllowsRole(string role) => AllowedRoles.Contains(role);
    public override IEnumerable<object> GetEqualityComponents() { foreach (var r in AllowedRoles) yield return r; }
}

public sealed class RoleName : ValueObject
{
    private static readonly HashSet<string> _valid = new() { "SystemAdministrator", "MoLAdministrator", "DataAnalyst", "EmployerOwner", "Auditor" };
    public string Value { get; }
    private RoleName(string value) => Value = value;
    public static Result<RoleName> Create(string value)
    {
        if (!_valid.Contains(value)) return Result.Failure<RoleName>(new Error("RoleName.Invalid", $"Unknown role: {value}"));
        return Result.Success(new RoleName(value));
    }
    public static RoleName SystemAdministrator => new("SystemAdministrator");
    public static RoleName MoLAdministrator => new("MoLAdministrator");
    public static RoleName DataAnalyst => new("DataAnalyst");
    public static RoleName EmployerOwner => new("EmployerOwner");
    public static RoleName Auditor => new("Auditor");
    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed class RoleScope : ValueObject
{
    public RoleName Role { get; }
    public Guid? EmployerId { get; }
    private RoleScope(RoleName role, Guid? employerId) { Role = role; EmployerId = employerId; }
    public static Result<RoleScope> Create(RoleName role, Guid? employerId)
    {
        if (role.Value == "EmployerOwner" && employerId is null)
            return Result.Failure<RoleScope>(new Error("RoleScope.MissingEmployerId", "EmployerId required for EmployerOwner."));
        return Result.Success(new RoleScope(role, employerId));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Role; yield return EmployerId ?? (object)"null"; }
}

public sealed class RunTrigger : ValueObject
{
    public TriggerMode Mode { get; }
    public Guid? UserId { get; }
    public Guid? ReportScheduleId { get; }
    private RunTrigger(TriggerMode mode, Guid? userId, Guid? scheduleId) { Mode = mode; UserId = userId; ReportScheduleId = scheduleId; }
    public static Result<RunTrigger> CreateOnDemand(Guid userId)
        => Result.Success(new RunTrigger(TriggerMode.OnDemand, userId, null));
    public static Result<RunTrigger> CreateScheduled(Guid scheduleId)
        => Result.Success(new RunTrigger(TriggerMode.Scheduled, null, scheduleId));
    public override IEnumerable<object> GetEqualityComponents() { yield return Mode; yield return UserId ?? (object)"null"; yield return ReportScheduleId ?? (object)"null"; }
}

public sealed class ScheduleCadence : ValueObject
{
    public Frequency Frequency { get; }
    public DayOfWeek? DayOfWeek { get; }
    public int? DayOfMonth { get; }
    public TimeOnly TimeOfDayUtc { get; }
    public IReadOnlyList<DateOnly> SkipDates { get; }
    private ScheduleCadence(Frequency frequency, DayOfWeek? dayOfWeek, int? dayOfMonth, TimeOnly timeOfDayUtc, List<DateOnly> skipDates)
    { Frequency = frequency; DayOfWeek = dayOfWeek; DayOfMonth = dayOfMonth; TimeOfDayUtc = timeOfDayUtc; SkipDates = skipDates.AsReadOnly(); }
    public static Result<ScheduleCadence> Create(Frequency frequency, DayOfWeek? dayOfWeek, int? dayOfMonth, TimeOnly timeOfDayUtc, List<DateOnly> skipDates)
    {
        if (frequency == Frequency.Weekly && dayOfWeek is null)
            return Result.Failure<ScheduleCadence>(new Error("ScheduleCadence.MissingDayOfWeek", "DayOfWeek required for weekly."));
        if ((frequency == Frequency.Monthly || frequency == Frequency.Quarterly) && (dayOfMonth is null || dayOfMonth < 1 || dayOfMonth > 28))
            return Result.Failure<ScheduleCadence>(new Error("ScheduleCadence.InvalidDayOfMonth", "DayOfMonth must be 1-28 for monthly/quarterly."));
        return Result.Success(new ScheduleCadence(frequency, dayOfWeek, dayOfMonth, timeOfDayUtc, skipDates));
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Frequency; yield return DayOfWeek ?? (object)"null"; yield return DayOfMonth ?? (object)"null"; yield return TimeOfDayUtc; }
}

public sealed class RetentionScope : ValueObject
{
    public ActorRole ActorRole { get; }
    public IReadOnlySet<ActivityType> ActivityTypes { get; }
    private RetentionScope(ActorRole role, HashSet<ActivityType> types) { ActorRole = role; ActivityTypes = types; }
    public static RetentionScope Create(ActorRole role, HashSet<ActivityType> types)
        => new(role, types);
    public override IEnumerable<object> GetEqualityComponents() { yield return ActorRole; foreach (var t in ActivityTypes) yield return t; }
}
