using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class Translation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TranscriptId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    public Transcript? Transcript { get; set; }
    public ICollection<CaptionFile> CaptionFiles { get; set; } = new List<CaptionFile>();
}
