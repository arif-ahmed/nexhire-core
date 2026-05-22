using System.Collections.Generic;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;

public class WithdrawalReason : ValueObject
{
    public string Code { get; }
    public string? Comment { get; }

    private static readonly HashSet<string> ValidCodes = new()
    {
        "ChangedMind",
        "AcceptedAnotherOffer",
        "NoLongerInterested",
        "RoleNotAsExpected",
        "AccountDeactivated"
    };

    private WithdrawalReason(string code, string? comment)
    {
        Code = code;
        Comment = comment;
    }

    public static Result<WithdrawalReason> Create(string code, string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<WithdrawalReason>(new Error("WithdrawalReason.EmptyCode", "Withdrawal reason code cannot be empty."));
        }

        if (!ValidCodes.Contains(code))
        {
            return Result.Failure<WithdrawalReason>(new Error("WithdrawalReason.InvalidCode", $"Withdrawal reason code '{code}' is invalid."));
        }

        if (comment?.Length > 1000)
        {
            return Result.Failure<WithdrawalReason>(new Error("WithdrawalReason.CommentTooLong", "Withdrawal reason comment must not exceed 1000 characters."));
        }

        return new WithdrawalReason(code, comment);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Comment ?? string.Empty;
    }
}
