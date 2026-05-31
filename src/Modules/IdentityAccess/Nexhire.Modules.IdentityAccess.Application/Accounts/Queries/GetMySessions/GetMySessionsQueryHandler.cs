using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMySessions;

public class GetMySessionsQueryHandler : IQueryHandler<GetMySessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public GetMySessionsQueryHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure<IReadOnlyList<SessionDto>>(new Error("E-NOT-FOUND", "Account not found."));

        var sessions = account.Sessions
            .Where(s => !s.IsRevoked && !s.IsExpired(DateTime.UtcNow))
            .Select(s => new SessionDto(
                s.Id.Value,
                s.Channel.ToString(),
                s.DeviceFingerprint.Value, // Assuming this is used as label for now
                s.IssuedOnUtc,
                s.IssuedOnUtc, // LastSeenUtc - not tracked yet, using IssuedOnUtc
                s.Id.Value == request.CurrentSessionId
            ))
            .ToList();

        return Result.Success<IReadOnlyList<SessionDto>>(sessions);
    }
}
