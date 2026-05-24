using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

namespace Nexhire.Api;

public class JobSeekerProfileQueryApiAdapter : IJobSeekerProfileQueryApi
{
    private readonly IJobSeekerProfileRepository _repository;

    public JobSeekerProfileQueryApiAdapter(IJobSeekerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<SeekerPersonalizationAttributes?> GetPersonalizationAttributesAsync(Guid userId, CancellationToken ct)
    {
        var profile = await _repository.GetByUserIdAsync(userId, ct);
        if (profile == null) return null;

        var sector = profile.Preferences?.Industries.FirstOrDefault();
        var location = profile.CurrentAddress?.City ?? profile.Preferences?.Locations.FirstOrDefault();
        var interests = profile.Preferences?.JobTypes.ToList() ?? new List<string>();

        return new SeekerPersonalizationAttributes(
            profile.UserId,
            sector,
            location,
            interests
        );
    }
}
