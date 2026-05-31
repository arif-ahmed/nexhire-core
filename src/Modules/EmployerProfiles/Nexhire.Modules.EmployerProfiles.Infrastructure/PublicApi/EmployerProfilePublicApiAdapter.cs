using Nexhire.Modules.EmployerProfiles.Contracts;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.PublicApi;

public class EmployerProfilePublicApiAdapter : IEmployerProfilePublicApi
{
    private readonly IEmployerProfileRepository _repository;

    public EmployerProfilePublicApiAdapter(IEmployerProfileRepository repository)
        => _repository = repository;

    public async Task<bool> IsVerifiedAsync(Guid employerUserId, CancellationToken ct)
    {
        var profile = await _repository.GetByUserIdAsync(employerUserId, ct);
        return profile?.IsVerified ?? false;
    }

    public async Task<EmployerProfileSummaryDto?> GetSummaryAsync(Guid employerUserId, CancellationToken ct)
    {
        var profile = await _repository.GetByUserIdAsync(employerUserId, ct);
        if (profile is null) return null;
        return new EmployerProfileSummaryDto(
            profile.Id,
            profile.UserId,
            profile.CompanyName.Value,
            profile.Status.ToString(),
            profile.IsVerified,
            profile.Logo?.StorageKey,
            profile.Industry);
    }
}
