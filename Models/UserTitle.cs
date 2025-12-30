using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class UserTitle
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Color { get; set; } = "#ffffff"; // CSS color code

    public int LevelRequirement { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLocked { get; set; } = false;
}
