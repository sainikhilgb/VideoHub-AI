using System.ComponentModel.DataAnnotations;
using VideoHub.Api.Domain.Media;

namespace VideoHub.Api.Domain.Entities;

public sealed class MediaFile
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = MediaFileTypes.Video;

    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Bucket { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string StoragePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Extension { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public TimeSpan? Duration { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = MediaFileStatuses.Pending;

    [MaxLength(128)]
    public string? Checksum { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public Project? Project { get; set; }
}
