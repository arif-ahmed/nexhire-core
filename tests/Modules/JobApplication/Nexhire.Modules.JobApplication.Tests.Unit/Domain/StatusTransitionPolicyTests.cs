using FluentAssertions;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Services;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.Domain;

public class StatusTransitionPolicyTests
{
    [Theory]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.UnderReview, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Shortlisted, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Rejected, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Withdrawn, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Expired, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Hired, false)] // Cannot go Submitted -> Hired directly
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Interview, true)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Submitted, false)] // Cannot go backwards
    [InlineData(ApplicationStatus.Hired, ApplicationStatus.UnderReview, false)] // Terminal state cannot transition out
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Shortlisted, false)] // Terminal state cannot transition out
    public void IsTransitionAllowed_Should_MatchStatusMatrix(ApplicationStatus from, ApplicationStatus to, bool expectedAllowed)
    {
        // Act
        var result = ApplicationStatusTransitionPolicy.IsTransitionAllowed(from, to);

        // Assert
        result.Should().Be(expectedAllowed);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.UnderReview, false)]
    [InlineData(ApplicationStatus.Hired, true)]
    [InlineData(ApplicationStatus.Rejected, true)]
    [InlineData(ApplicationStatus.Withdrawn, true)]
    [InlineData(ApplicationStatus.Expired, true)]
    public void IsTerminal_Should_IdentifyTerminalStatuses(ApplicationStatus status, bool expectedTerminal)
    {
        // Act
        var result = ApplicationStatusTransitionPolicy.IsTerminal(status);

        // Assert
        result.Should().Be(expectedTerminal);
    }
}
