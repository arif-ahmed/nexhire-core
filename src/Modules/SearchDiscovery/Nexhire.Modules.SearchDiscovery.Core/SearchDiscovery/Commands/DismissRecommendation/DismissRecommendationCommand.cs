using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DismissRecommendation;

public record DismissRecommendationCommand(
    Guid SeekerUserId,
    Guid PostingId) : ICommand;
