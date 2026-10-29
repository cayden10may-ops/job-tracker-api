using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetAll(
        [FromQuery] string? search, CancellationToken ct)
    {
        var query = db.Companies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.Location, c.Applications.Count))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyResponse>> GetById(int id, CancellationToken ct)
    {
        var company = await db.Companies.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.Location, c.Applications.Count))
            .FirstOrDefaultAsync(ct);

        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CompanyRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await db.Companies.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
        {
            return Problem(
                title: "Company already exists",
                detail: $"A company named '{name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var company = new Company
        {
            Name = name,
            Website = request.Website,
            Location = request.Location
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);

        var response = new CompanyResponse(company.Id, company.Name, company.Website, company.Location, 0);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CompanyRequest request, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync(new object[] { id }, ct);
        if (company is null) return NotFound();

        var name = request.Name.Trim();
        if (await db.Companies.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct))
        {
            return Problem(
                title: "Company already exists",
                detail: $"A company named '{name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        company.Name = name;
        company.Website = request.Website;
        company.Location = request.Location;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync(new object[] { id }, ct);
        if (company is null) return NotFound();

        if (await db.JobApplications.AnyAsync(a => a.CompanyId == id, ct))
        {
            return Problem(
                title: "Company has applications",
                detail: "Delete or move this company's applications before deleting the company.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Companies.Remove(company);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}