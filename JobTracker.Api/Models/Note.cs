namespace JobTracker.Api.Models;

public class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }
}