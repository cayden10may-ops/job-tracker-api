namespace JobTracker.Api.Models;

public class JobApplication
{
    public int Id { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateOnly AppliedOn { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? JobAdUrl { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public List<Note> Notes { get; set; } = new();
    public List<StatusChange> StatusHistory { get; set; } = new();
}