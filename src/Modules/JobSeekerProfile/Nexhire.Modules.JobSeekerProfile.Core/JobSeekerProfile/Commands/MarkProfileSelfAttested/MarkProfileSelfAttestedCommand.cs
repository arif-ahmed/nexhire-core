using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.MarkProfileSelfAttested;

public record MarkProfileSelfAttestedCommand(Guid UserId) : ICommand;
