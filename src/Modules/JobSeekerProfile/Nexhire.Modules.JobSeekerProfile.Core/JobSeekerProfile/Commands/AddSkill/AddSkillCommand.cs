using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddSkill;

public record AddSkillCommand(
    Guid UserId,
    string RawLabel,
    string Category,
    string Tier,
    int Proficiency) : ICommand;
