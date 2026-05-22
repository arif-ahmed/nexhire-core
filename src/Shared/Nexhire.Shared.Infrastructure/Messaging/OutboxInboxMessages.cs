using Microsoft.EntityFrameworkCore;

namespace Nexhire.Shared.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public void MarkProcessed(DateTime processedOnUtc) => ProcessedOnUtc = processedOnUtc;
    public void MarkFailed(string error) => Error = error;
}

public sealed class InboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTime ReceivedOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }

    private InboxMessage() { }

    public InboxMessage(Guid id, string type, DateTime receivedOnUtc)
    {
        Id = id;
        Type = type;
        ReceivedOnUtc = receivedOnUtc;
    }

    public void MarkProcessed(DateTime processedOnUtc) => ProcessedOnUtc = processedOnUtc;
}

public interface IOutboxInboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }
}
