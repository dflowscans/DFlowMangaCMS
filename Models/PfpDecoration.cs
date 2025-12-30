using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class PfpDecoration
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int LevelRequirement { get; set; } = 1;

    public bool IsAnimated { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLocked { get; set; } = false;
}
