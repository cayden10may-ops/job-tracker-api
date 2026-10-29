using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/applications/{applicationId:int}/notes")]
public class NotesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NoteResponse>>> GetAll(int applicationId, CancellationToken ct)
    {
        if (!await db.JobApplications.AnyAsync(a => a.Id == applicationId, ct))
            return NotFound();

        var notes = await db.Notes.AsNoTracking()
            .Where(n => n.JobApplicationId == applicationId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteResponse(n.Id, n.Text, n.CreatedAt))
            .ToListAsync(ct);

        return Ok(notes);
    }

    [HttpPost]
    public async Task<ActionResult<NoteResponse>> Create(
        int applicationId, NoteRequest request, CancellationToken ct)
    {
        if (!await db.JobApplications.AnyAsync(a => a.Id == applicationId, ct))
            return NotFound();

        var note = new Note
        {
            JobApplicationId = applicationId,
            Text = request.Text.Trim()
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

        var response = new NoteResponse(note.Id, note.Text, note.CreatedAt);
        return CreatedAtAction(nameof(GetAll), new { applicationId }, response);
    }

    [HttpDelete("{noteId:int}")]
    public async Task<IActionResult> Delete(int applicationId, int noteId, CancellationToken ct)
    {
        var note = await db.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.JobApplicationId == applicationId, ct);
        if (note is null) return NotFound();

        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}