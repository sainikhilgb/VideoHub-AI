using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class CombinedMedia
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid MediaFileId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string MuxType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued"; // "Queued", "Processing", "Completed", "Failed"

    [MaxLength(2048)]
    public string? BlobUrl { get; set; }

    public string? Error { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Project? Project { get; set; }
    public MediaFile? MediaFile { get; set; }
}
