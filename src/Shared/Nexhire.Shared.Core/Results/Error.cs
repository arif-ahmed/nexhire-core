namespace Nexhire.Shared.Core.Results;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified value is null.");
    public static readonly Error ValidationError = new("Error.ValidationError", "A validation error occurred.");
}
