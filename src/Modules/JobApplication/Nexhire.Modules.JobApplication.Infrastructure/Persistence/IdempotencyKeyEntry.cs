using System;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence;

public sealed class IdempotencyKeyEntry
{
    public Guid Key { get; set; }
    public Guid ApplicationId { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
