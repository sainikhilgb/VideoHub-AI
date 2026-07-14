using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoHub.Api.Domain.Entities;

public sealed class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedByIp { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    [MaxLength(200)]
    public string? Device { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
