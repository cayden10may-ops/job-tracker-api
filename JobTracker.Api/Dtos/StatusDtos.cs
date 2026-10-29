using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Models;

namespace JobTracker.Api.Dtos;

public class StatusUpdateRequest
{
    [Required]
    public ApplicationStatus? Status { get; set; }
}

public record StatusChangeResponse(
    ApplicationStatus From,
    ApplicationStatus To,
    DateTime ChangedAt);