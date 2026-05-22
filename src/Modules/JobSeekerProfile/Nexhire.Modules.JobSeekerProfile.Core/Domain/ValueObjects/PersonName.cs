using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class PersonName : ValueObject
{
    public string First { get; }
    public string Last { get; }

    private PersonName(string first, string last)
    {
        First = first;
        Last = last;
    }

    public static Result<PersonName> Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
        {
            return Result.Failure<PersonName>(new Error("PersonName.Empty", "First and last name cannot be empty."));
        }

        var trimmedFirst = first.Trim();
        var trimmedLast = last.Trim();

        if (trimmedFirst.Length > 100 || trimmedLast.Length > 100)
        {
            return Result.Failure<PersonName>(new Error("PersonName.TooLong", "Names must not exceed 100 characters."));
        }

        return Result.Success(new PersonName(trimmedFirst, trimmedLast));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return First;
        yield return Last;
    }
}
