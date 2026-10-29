using System.Linq.Expressions;
using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using JobTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController(AppDbContext db) : ControllerBase
{
    // One projection reused by every endpoint, translated into SQL by EF Core
    private static readonly Expression<Func<JobApplication, ApplicationResponse>> ToResponse =
        a => new ApplicationResponse(
            a.Id, a.RoleTitle, a.Status, a.AppliedOn, a.SalaryMin, a.SalaryMax,
            a.JobAdUrl, a.CompanyId, a.Company!.Name, a.Notes.Count);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationResponse>>> GetAll(
        [FromQuery] ApplicationQuery q, CancellationToken ct)
    {
        var query = db.JobApplications.AsNoTracking().AsQueryable();

        if (q.Status.HasValue)
            query = query.Where(a => a.Status == q.Status.Value);

        if (q.CompanyId.HasValue)
            query = query.Where(a => a.CompanyId == q.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = $"%{q.Search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.Like(a.RoleTitle, pattern) ||
                EF.Functions.Like(a.Company!.Name, pattern));
        }

        query = q.Sort switch
        {
            "oldest" => query.OrderBy(a => a.AppliedOn).ThenBy(a => a.Id),
            "company" => query.OrderBy(a => a.Company!.Name).ThenByDescending(a => a.AppliedOn),
            "role" => query.OrderBy(a => a.RoleTitle),
            _ => query.OrderByDescending(a => a.AppliedOn).ThenByDescending(a => a.Id)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(ToResponse)
            .ToListAsync(ct);

        return Ok(new PagedResult<ApplicationResponse>(items, q.Page, q.PageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id, CancellationToken ct)
    {
        var application = await db.JobApplications.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(ct);

        return application is null ? NotFound() : Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(
        ApplicationRequest request, CancellationToken ct)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        var application = new JobApplication
        {
            RoleTitle = request.RoleTitle.Trim(),
            CompanyId = request.CompanyId,
            AppliedOn = request.AppliedOn ?? DateOnly.FromDateTime(DateTime.UtcNow),
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            JobAdUrl = request.JobAdUrl,
            Status = ApplicationStatus.Applied
        };

        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);

        var response = await db.JobApplications.AsNoTracking()
            .Where(a => a.Id == application.Id)
            .Select(ToResponse)
            .FirstAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = application.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ApplicationRequest request, CancellationToken ct)
    {
        var application = await db.JobApplications.FindAsync(new object[] { id }, ct);
        if (application is null) return NotFound();

        if (!await db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        application.RoleTitle = request.RoleTitle.Trim();
        application.CompanyId = request.CompanyId;
        application.AppliedOn = request.AppliedOn ?? application.AppliedOn;
        application.SalaryMin = request.SalaryMin;
        application.SalaryMax = request.SalaryMax;
        application.JobAdUrl = request.JobAdUrl;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApplicationResponse>> ChangeStatus(
        int id, StatusUpdateRequest request, CancellationToken ct)
    {
        var application = await db.JobApplications.FindAsync(new object[] { id }, ct);
        if (application is null) return NotFound();

        var target = request.Status!.Value;

        if (target == application.Status)
        {
            return Problem(
                title: "Status unchanged",
                detail: $"The application is already '{target}'.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (!ApplicationWorkflow.CanTransition(application.Status, target))
        {
            var allowed = ApplicationWorkflow.NextStatuses(application.Status);
            var options = allowed.Count == 0
                ? "none (this is a final status)"
                : string.Join(", ", allowed);

            return Problem(
                title: "Invalid status change",
                detail: $"Cannot move from '{application.Status}' to '{target}'. Allowed next statuses: {options}.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.StatusChanges.Add(new StatusChange
        {
            JobApplicationId = id,
            FromStatus = application.Status,
            ToStatus = target
        });
        application.Status = target;

        await db.SaveChangesAsync(ct);

        var response = await db.JobApplications.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ToResponse)
            .FirstAsync(ct);

        return Ok(response);
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<StatusChangeResponse>>> GetHistory(int id, CancellationToken ct)
    {
        if (!await db.JobApplications.AnyAsync(a => a.Id == id, ct))
            return NotFound();

        var history = await db.StatusChanges.AsNoTracking()
            .Where(s => s.JobApplicationId == id)
            .OrderBy(s => s.ChangedAt)
            .Select(s => new StatusChangeResponse(s.FromStatus, s.ToStatus, s.ChangedAt))
            .ToListAsync(ct);

        return Ok(history);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var application = await db.JobApplications.FindAsync(new object[] { id }, ct);
        if (application is null) return NotFound();

        db.JobApplications.Remove(application); // notes and history are deleted too (cascade)
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}