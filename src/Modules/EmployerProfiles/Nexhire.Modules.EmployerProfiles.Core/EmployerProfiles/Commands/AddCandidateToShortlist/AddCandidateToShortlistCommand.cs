using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.AddCandidateToShortlist;

public record AddCandidateToShortlistCommand(Guid UserId, Guid ShortlistId, Guid CandidateUserId, int? MatchScore = null) : ICommand;
