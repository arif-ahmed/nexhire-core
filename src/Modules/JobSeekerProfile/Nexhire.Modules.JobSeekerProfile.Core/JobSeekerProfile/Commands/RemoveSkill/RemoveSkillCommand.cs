using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveSkill;

public record RemoveSkillCommand(Guid UserId, Guid SkillId) : ICommand;
