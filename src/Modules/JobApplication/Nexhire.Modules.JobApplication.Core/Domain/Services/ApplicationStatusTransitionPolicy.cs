using System;
using System.Collections.Generic;

namespace Nexhire.Modules.JobApplication.Core.Domain.Services;

public static class ApplicationStatusTransitionPolicy
{
    private static readonly Dictionary<ApplicationStatus, HashSet<ApplicationStatus>> AllowedTransitions = new()
    {
        {
            ApplicationStatus.Submitted, new HashSet<ApplicationStatus>
            {
                ApplicationStatus.UnderReview,
                ApplicationStatus.Shortlisted,
                ApplicationStatus.Rejected,
                ApplicationStatus.Withdrawn,
                ApplicationStatus.Expired
            }
        },
        {
            ApplicationStatus.UnderReview, new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Shortlisted,
                ApplicationStatus.Interview,
                ApplicationStatus.Rejected,
                ApplicationStatus.Withdrawn,
                ApplicationStatus.Expired
            }
        },
        {
            ApplicationStatus.Shortlisted, new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Interview,
                ApplicationStatus.Offered,
                ApplicationStatus.Rejected,
                ApplicationStatus.Withdrawn,
                ApplicationStatus.Expired
            }
        },
        {
            ApplicationStatus.Interview, new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Offered,
                ApplicationStatus.Rejected,
                ApplicationStatus.Withdrawn,
                ApplicationStatus.Expired
            }
        },
        {
            ApplicationStatus.Offered, new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Hired,
                ApplicationStatus.Rejected,
                ApplicationStatus.Withdrawn,
                ApplicationStatus.Expired
            }
        },
        // Terminal states have no legal outgoing transitions
        { ApplicationStatus.Hired, new HashSet<ApplicationStatus>() },
        { ApplicationStatus.Rejected, new HashSet<ApplicationStatus>() },
        { ApplicationStatus.Withdrawn, new HashSet<ApplicationStatus>() },
        { ApplicationStatus.Expired, new HashSet<ApplicationStatus>() }
    };

    public static bool IsTransitionAllowed(ApplicationStatus from, ApplicationStatus to)
    {
        if (from == to)
        {
            return true; // Self-transition is technically idempotent in some contexts, but let's check
        }

        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static bool IsTerminal(ApplicationStatus status)
    {
        return status is ApplicationStatus.Hired 
                      or ApplicationStatus.Rejected 
                      or ApplicationStatus.Withdrawn 
                      or ApplicationStatus.Expired;
    }

    public static bool RequiresReason(ApplicationStatus to)
    {
        return to is ApplicationStatus.Rejected or ApplicationStatus.Withdrawn;
    }
}
