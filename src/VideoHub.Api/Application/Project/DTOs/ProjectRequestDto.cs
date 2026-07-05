using System.ComponentModel.DataAnnotations;

public class ProjectRequestDto
{
     [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string OriginalLanguage { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }


    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
