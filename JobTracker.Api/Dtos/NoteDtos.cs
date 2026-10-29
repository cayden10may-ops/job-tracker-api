using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Dtos;

public class NoteRequest
{
    [Required, StringLength(2000)]
    public string Text { get; set; } = string.Empty;
}

public record NoteResponse(int Id, string Text, DateTime CreatedAt);