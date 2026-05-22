using System;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobApplication.Core.Domain;

public class ApplicationStage : Entity<Guid>
{
    public ApplicationStatus Stage { get; private set; }
    public DateTime EnteredOnUtc { get; private set; }
    public StageActorRole EnteredByRole { get; private set; }
    public Guid? EnteredByUserId { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Comment { get; private set; }

    private ApplicationStage(
        Guid id,
        ApplicationStatus stage,
        DateTime enteredOnUtc,
        StageActorRole enteredByRole,
        Guid? enteredByUserId,
        string? reasonCode,
        string? comment) : base(id)
    {
        Stage = stage;
        EnteredOnUtc = enteredOnUtc;
        EnteredByRole = enteredByRole;
        EnteredByUserId = enteredByUserId;
        ReasonCode = reasonCode;
        Comment = comment;
    }

    private ApplicationStage()
    {
        // Required by EF Core
    }

    public static ApplicationStage Create(
        ApplicationStatus stage,
        DateTime enteredOnUtc,
        StageActorRole enteredByRole,
        Guid? enteredByUserId = null,
        string? reasonCode = null,
        string? comment = null)
    {
        return new ApplicationStage(
            Guid.NewGuid(),
            stage,
            enteredOnUtc,
            enteredByRole,
            enteredByUserId,
            reasonCode,
            comment);
    }
}
