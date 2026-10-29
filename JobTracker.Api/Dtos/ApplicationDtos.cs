using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Models;

namespace JobTracker.Api.Dtos;

public class ApplicationRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string RoleTitle { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "CompanyId must be a valid company id.")]
    public int CompanyId { get; set; }

    public DateOnly? AppliedOn { get; set; }

    [Range(0, 10_000_000)]
    public int? SalaryMin { get; set; }

    [Range(0, 10_000_000)]
    public int? SalaryMax { get; set; }

    [StringLength(500), Url]
    public string? JobAdUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMax < SalaryMin)
        {
            yield return new ValidationResult(
                "SalaryMax cannot be lower than SalaryMin.", new[] { nameof(SalaryMax) });
        }

        // Allow up to tomorrow (UTC) so users in earlier time zones aren't rejected
        if (AppliedOn.HasValue && AppliedOn.Value > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
        {
            yield return new ValidationResult(
                "AppliedOn cannot be in the future.", new[] { nameof(AppliedOn) });
        }
    }
}

public class ApplicationQuery
{
    public ApplicationStatus? Status { get; set; }
    public int? CompanyId { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    [RegularExpression("^(newest|oldest|company|role)$",
        ErrorMessage = "Sort must be one of: newest, oldest, company, role.")]
    public string Sort { get; set; } = "newest";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public record ApplicationResponse(
    int Id,
    string RoleTitle,
    ApplicationStatus Status,
    DateOnly AppliedOn,
    int? SalaryMin,
    int? SalaryMax,
    string? JobAdUrl,
    int CompanyId,
    string CompanyName,
    int NoteCount);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}