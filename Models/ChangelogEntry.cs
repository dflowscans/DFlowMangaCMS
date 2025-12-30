using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class ChangelogEntry
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
