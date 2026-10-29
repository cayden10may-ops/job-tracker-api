using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Dtos;

public class CompanyRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300), Url]
    public string? Website { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }
}

public record CompanyResponse(
    int Id,
    string Name,
    string? Website,
    string? Location,
    int ApplicationCount);