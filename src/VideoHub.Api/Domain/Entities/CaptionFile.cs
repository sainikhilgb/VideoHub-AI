using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class CaptionFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid? JobId { get; set; }

    public Guid? TranscriptId { get; set; }
    public Guid? TranslationId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Format { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Style { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    // Nullable: populated after Python uploads the file successfully
    [MaxLength(2048)]
    public string? BlobUrl { get; set; }

    public Job? Job { get; set; }
    public Transcript? Transcript { get; set; }
    public Translation? Translation { get; set; }
}
