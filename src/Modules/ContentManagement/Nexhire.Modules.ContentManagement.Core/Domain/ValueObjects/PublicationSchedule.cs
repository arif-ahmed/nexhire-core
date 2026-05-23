using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class PublicationSchedule : ValueObject
{
    public DateTime PublishAtUtc { get; }

    private PublicationSchedule(DateTime publishAtUtc)
    {
        PublishAtUtc = publishAtUtc;
    }

    public static Result<PublicationSchedule> Create(DateTime publishAtUtc)
    {
        if (publishAtUtc <= DateTime.UtcNow)
            return Result.Failure<PublicationSchedule>(new Error("E-SCHEDULE-PAST", "Publication time must be in the future."));

        return Result.Success(new PublicationSchedule(publishAtUtc));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PublishAtUtc;
    }
}
