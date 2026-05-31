using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RemoveCandidateFromShortlist;

public record RemoveCandidateFromShortlistCommand(Guid UserId, Guid ShortlistId, Guid ShortlistMemberId) : ICommand;
