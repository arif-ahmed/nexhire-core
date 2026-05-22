using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobPostings.Core.JobPostings.Commands;

public sealed record CreateJobPostingCommand(Guid EmployerId, Guid PostedByUserId, JobPostingDraftDto Draft) : ICommand<Guid>;
public sealed record UpdateJobPostingDetailsCommand(Guid JobPostingId, Guid EmployerId, JobPostingDraftDto Draft) : ICommand;
public sealed record UpdateRequiredSkillsCommand(Guid JobPostingId, Guid EmployerId, IReadOnlyCollection<SkillInput> RequiredSkills) : ICommand;
public sealed record ExtendApplicationDeadlineCommand(Guid JobPostingId, Guid EmployerId, DateTime NewDeadlineUtc, bool AutoCloseEnabled) : ICommand;
public sealed record SetPostingVisibilityCommand(Guid JobPostingId, Guid EmployerId, PostingVisibilityDto Visibility) : ICommand;
public sealed record PublishJobPostingCommand(Guid JobPostingId, Guid EmployerId, Guid UserId) : ICommand;
public sealed record PauseJobPostingCommand(Guid JobPostingId, Guid EmployerId, Guid UserId) : ICommand;
public sealed record ResumeJobPostingCommand(Guid JobPostingId, Guid EmployerId, Guid UserId) : ICommand;
public sealed record ArchiveJobPostingCommand(Guid JobPostingId, Guid EmployerId, Guid UserId) : ICommand;
public sealed record RenewJobPostingCommand(Guid JobPostingId, Guid EmployerId, Guid UserId, DateTime NewDeadlineUtc, bool AutoCloseEnabled) : ICommand<Guid>;
public sealed record BulkRenewJobPostingsCommand(Guid EmployerId, Guid UserId, IReadOnlyCollection<Guid> JobPostingIds, DateTime NewDeadlineUtc, bool AutoCloseEnabled) : ICommand<IReadOnlyCollection<BulkRenewResultDto>>;
public sealed record SuspendJobPostingCommand(Guid JobPostingId, Guid AdminUserId, string Reason) : ICommand;
public sealed record ReinstateJobPostingCommand(Guid JobPostingId, Guid AdminUserId) : ICommand;
public sealed record RemoveJobPostingCommand(Guid JobPostingId, Guid AdminUserId, string Reason) : ICommand;
public sealed record ProcessExpiredPostingsCommand(DateTime NowUtc) : ICommand<int>;
