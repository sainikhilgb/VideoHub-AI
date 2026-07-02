using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class Transcript
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;

    [Required]
    public int Version { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    public Project? Project { get; set; }
    public ICollection<TranscriptSegment> Segments { get; set; } = new List<TranscriptSegment>();
    public ICollection<Speaker> Speakers { get; set; } = new List<Speaker>();
    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
