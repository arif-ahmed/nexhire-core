using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.AddFavoriteJob;

public record AddFavoriteJobCommand(
    Guid SeekerUserId,
    Guid PostingId) : ICommand<Guid>;
