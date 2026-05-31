namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public sealed class AdminActionLog
{
    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public AdminActionType ActionType { get; private set; }
    public Guid TargetUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    private AdminActionLog() { }

    public static AdminActionLog Record(Guid adminUserId, AdminActionType actionType, Guid targetUserId, string? reason)
    {
        return new AdminActionLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetUserId = targetUserId,
            Reason = reason,
            OccurredOnUtc = DateTime.UtcNow
        };
    }
}
