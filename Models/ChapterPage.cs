using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class ChapterPage
{
    public int Id { get; set; }

    public int ChapterId { get; set; }

    [Required(ErrorMessage = "Page number is required")]
    public int PageNumber { get; set; }

    [Required(ErrorMessage = "Image URL is required")]
    [StringLength(500)]
    public required string ImageUrl { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ChapterId")]
    public Chapter? Chapter { get; set; }
}
