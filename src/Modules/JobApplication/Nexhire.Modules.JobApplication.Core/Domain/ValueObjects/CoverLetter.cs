using System.Collections.Generic;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;

public class CoverLetter : ValueObject
{
    public string Text { get; }

    private CoverLetter(string text)
    {
        Text = text;
    }

    public static Result<CoverLetter> Create(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<CoverLetter>(new Error("CoverLetter.Empty", "Cover letter text cannot be empty."));
        }

        if (text.Length > 4000)
        {
            return Result.Failure<CoverLetter>(new Error("CoverLetter.TooLong", "Cover letter must not exceed 4000 characters."));
        }

        return new CoverLetter(text);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Text;
    }
}
