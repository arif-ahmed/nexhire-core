using System;

namespace Nexhire.Modules.JobApplication.Core.Domain;

public record BookmarkId(Guid Value)
{
    public static BookmarkId New() => new(Guid.NewGuid());
    public static BookmarkId Empty => new(Guid.Empty);
    
    public override string ToString() => Value.ToString();
}
