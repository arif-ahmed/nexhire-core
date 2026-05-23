using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class FaqContent : ValueObject
{
    private static readonly Regex ScriptTagRegex = new(
        @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*</style>|\bon\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Question { get; }
    public string AnswerRichText { get; }

    private FaqContent(string question, string answerRichText)
    {
        Question = question;
        AnswerRichText = answerRichText;
    }

    public static Result<FaqContent> Create(string question, string answerRichText)
    {
        if (string.IsNullOrWhiteSpace(question))
            return Result.Failure<FaqContent>(new Error("E-FAQ-QUESTION-EMPTY", "Question cannot be empty."));

        if (question.Length > 300)
            return Result.Failure<FaqContent>(new Error("E-FAQ-QUESTION-TOO-LONG", "Question cannot exceed 300 characters."));

        if (string.IsNullOrWhiteSpace(answerRichText))
            return Result.Failure<FaqContent>(new Error("E-FAQ-ANSWER-EMPTY", "Answer cannot be empty."));

        var sanitized = ScriptTagRegex.Replace(answerRichText, string.Empty);

        return Result.Success(new FaqContent(question.Trim(), sanitized));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Question;
        yield return AnswerRichText;
    }
}
