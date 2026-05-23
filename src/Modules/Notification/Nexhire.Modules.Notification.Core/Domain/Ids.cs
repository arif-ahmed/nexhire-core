using System;

namespace Nexhire.Modules.Notification.Core.Domain;

public record NotificationId(Guid Value)
{
    public static NotificationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record RecipientPreferencesId(Guid Value)
{
    public static RecipientPreferencesId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record NotificationTemplateId(Guid Value)
{
    public static NotificationTemplateId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record DigestId(Guid Value)
{
    public static DigestId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
