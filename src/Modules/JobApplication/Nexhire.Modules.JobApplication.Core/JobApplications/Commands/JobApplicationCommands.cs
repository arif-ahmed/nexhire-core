using System;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobApplication.Core.JobApplications.Commands;

public sealed record AddBookmarkCommand(Guid JobSeekerId, Guid JobPostingId) : ICommand<Guid>;

public sealed record RemoveBookmarkCommand(Guid JobSeekerId, Guid JobPostingId) : ICommand;

public sealed record SubmitApplicationCommand(
    Guid JobSeekerId,
    Guid JobPostingId,
    Guid ResumeDocumentId,
    string? CoverLetter,
    SnapshotOverrides? Overrides,
    Guid IdempotencyKey
) : ICommand<SubmitApplicationResponse>;

public sealed record SubmitApplicationResponse(
    Guid ApplicationId,
    string Status,
    int? MatchScoreAtApply
);

public sealed record WithdrawApplicationCommand(
    Guid ApplicationId,
    Guid JobSeekerId,
    string ReasonCode,
    string? Comment
) : ICommand<WithdrawApplicationResponse>;

public sealed record WithdrawApplicationResponse(
    string Status,
    DateTime WithdrawnOnUtc
);
