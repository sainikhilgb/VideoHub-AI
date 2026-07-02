using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class Word
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SegmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Text { get; set; } = string.Empty;

    public double Start { get; set; }
    public double End { get; set; }

    public double? Confidence { get; set; }

    public TranscriptSegment? Segment { get; set; }
}
