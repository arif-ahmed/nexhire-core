using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RemoveFavoriteJob;

public record RemoveFavoriteJobCommand(
    Guid SeekerUserId,
    Guid PostingId) : ICommand;
