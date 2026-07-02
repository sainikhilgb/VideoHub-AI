using System.ComponentModel.DataAnnotations;

namespace VideoHub.Api.Domain.Entities;

public sealed class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
