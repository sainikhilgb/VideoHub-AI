using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class Speaker
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TranscriptId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SpeakerLabel { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Name { get; set; }

    public double? Confidence { get; set; }

    public Transcript? Transcript { get; set; }
}
