using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Configurations;

internal static class JsonConversion
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };

    public static ValueConverter<T, string> Converter<T>() =>
        new(v => JsonSerializer.Serialize(v, Options), v => JsonSerializer.Deserialize<T>(v, Options)!);

    public static ValueConverter<T?, string?> NullableConverter<T>() where T : class =>
        new(v => v == null ? null : JsonSerializer.Serialize(v, Options), v => string.IsNullOrWhiteSpace(v) ? null : JsonSerializer.Deserialize<T>(v, Options));
}
