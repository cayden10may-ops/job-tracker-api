using JobTracker.Api.Models;
using JobTracker.Api.Services;

namespace JobTracker.Tests;

public class ApplicationWorkflowTests
{
    [Theory]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Rejected)]
    public void Allows_valid_transitions(ApplicationStatus from, ApplicationStatus to)
    {
        Assert.True(ApplicationWorkflow.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Offer)]
    public void Blocks_invalid_transitions(ApplicationStatus from, ApplicationStatus to)
    {
        Assert.False(ApplicationWorkflow.CanTransition(from, to));
    }

    [Fact]
    public void Rejected_is_a_final_status()
    {
        Assert.Empty(ApplicationWorkflow.NextStatuses(ApplicationStatus.Rejected));
    }
}