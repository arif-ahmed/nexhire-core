using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyMfaChallenge;

public record VerifyMfaChallengeCommand(
    Guid ChallengeId,
    string Code,
    string Channel,
    string DeviceFingerprint,
    string IpAddress) : ICommand<LoginResultDto>;
