using System;

namespace Nexhire.Modules.JobApplication.Core.Domain;

public record ApplicationId(Guid Value)
{
    public static ApplicationId New() => new(Guid.NewGuid());
    public static ApplicationId Empty => new(Guid.Empty);
    
    public override string ToString() => Value.ToString();
}
