using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatsResponse>> Get(CancellationToken ct)
    {
        var total = await db.JobApplications.CountAsync(ct);

        var grouped = await db.JobApplications.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byStatus = Enum.GetValues<ApplicationStatus>().ToDictionary(
            s => s.ToString(),
            s => grouped.FirstOrDefault(g => g.Status == s)?.Count ?? 0);

        // Use the history so applications that were rejected AFTER an interview still count
        var interviewRows = await db.StatusChanges.AsNoTracking()
            .Where(c => c.ToStatus == ApplicationStatus.Interview)
            .Select(c => new { c.JobApplicationId, c.JobApplication!.AppliedOn, c.ChangedAt })
            .ToListAsync(ct);

        var reachedInterview = interviewRows.Select(r => r.JobApplicationId).Distinct().Count();

        var receivedOffer = await db.StatusChanges.AsNoTracking()
            .Where(c => c.ToStatus == ApplicationStatus.Offer)
            .Select(c => c.JobApplicationId)
            .Distinct()
            .CountAsync(ct);

        double? averageDays = interviewRows.Count == 0
            ? null
            : Math.Round(interviewRows.Average(r =>
                Math.Max(0, DateOnly.FromDateTime(r.ChangedAt).DayNumber - r.AppliedOn.DayNumber)), 1);

        double? Percent(int part) => total == 0 ? null : Math.Round(part * 100.0 / total, 1);

        return Ok(new StatsResponse(
            total, byStatus, reachedInterview, receivedOffer,
            Percent(reachedInterview), Percent(receivedOffer), averageDays));
    }
}