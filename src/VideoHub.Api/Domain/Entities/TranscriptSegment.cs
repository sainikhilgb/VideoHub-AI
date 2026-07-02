using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class TranscriptSegment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TranscriptId { get; set; }

    public double StartTime { get; set; }
    public double EndTime { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public Guid? SpeakerId { get; set; }
    public double? Confidence { get; set; }

    public Transcript? Transcript { get; set; }
    public Speaker? Speaker { get; set; }
    public ICollection<Word> Words { get; set; } = new List<Word>();
}
