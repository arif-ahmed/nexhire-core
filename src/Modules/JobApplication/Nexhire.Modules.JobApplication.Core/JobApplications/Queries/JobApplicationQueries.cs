using System;
using System.Collections.Generic;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobApplication.Core.JobApplications.Queries;

public sealed record GetMyBookmarksQuery(Guid JobSeekerId) : IQuery<IReadOnlyCollection<BookmarkedJobDto>>;

public sealed record GetMyApplicationsQuery(
    Guid JobSeekerId,
    string? Status = null,
    int Page = 1,
    int PageSize = 10
) : IQuery<PagedResult<ApplicationListItemDto>>;

public sealed record GetMyApplicationDetailQuery(Guid ApplicationId, Guid JobSeekerId) : IQuery<ApplicationDetailDto>;
