using JobTracker.Api.Models;

namespace JobTracker.Api.Services;

public static class ApplicationWorkflow
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> Allowed = new()
    {
        [ApplicationStatus.Applied] = [ApplicationStatus.Interview, ApplicationStatus.Rejected],
        [ApplicationStatus.Interview] = [ApplicationStatus.Offer, ApplicationStatus.Rejected],
        [ApplicationStatus.Offer] = [ApplicationStatus.Rejected],
        [ApplicationStatus.Rejected] = []
    };

    public static IReadOnlyList<ApplicationStatus> NextStatuses(ApplicationStatus current) =>
        Allowed[current];

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to) =>
        Allowed[from].Contains(to);
}