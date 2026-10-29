namespace JobTracker.Api.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Location { get; set; }

    public List<JobApplication> Applications { get; set; } = new();
}