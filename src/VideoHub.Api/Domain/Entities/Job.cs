using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class Job
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    public int Attempts { get; set; }

    [MaxLength(500)]
    public string? StatusMessage { get; set; }

    [MaxLength(200)]
    public string TargetLanguages { get; set; } = string.Empty;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Project? Project { get; set; }
}
