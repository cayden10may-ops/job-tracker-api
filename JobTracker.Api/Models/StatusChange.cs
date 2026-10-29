namespace JobTracker.Api.Models;

public class StatusChange
{
    public int Id { get; set; }
    public ApplicationStatus FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }
}