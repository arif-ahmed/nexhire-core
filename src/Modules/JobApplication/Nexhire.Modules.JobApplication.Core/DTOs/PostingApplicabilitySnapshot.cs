using System;

namespace Nexhire.Modules.JobApplication.Core.DTOs;

public record PostingApplicabilitySnapshot(
    Guid JobPostingId,
    Guid EmployerId,
    string Status,
    DateTime? DeadlineUtc);
