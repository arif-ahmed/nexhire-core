using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.ConfirmParsedResumeFields;

public record ConfirmParsedResumeFieldsCommand(
    Guid UserId,
    Guid ResumeId,
    IReadOnlyCollection<string> SelectedFieldKeys) : ICommand;
