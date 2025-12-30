using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class Chapter
{
    public int Id { get; set; }

    public int MangaId { get; set; }

    [Required(ErrorMessage = "Chapter number is required")]
    public decimal ChapterNumber { get; set; } // Supports 1, 1.1, 1.2, 4.5 etc

    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? CoverImageUrl { get; set; }

    public DateTime ReleasedDate { get; set; } = DateTime.UtcNow;

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("MangaId")]
    public virtual Manga? Manga { get; set; }

    public ICollection<ChapterPage> Pages { get; set; } = new List<ChapterPage>();
    public ICollection<ChapterComment> Comments { get; set; } = new List<ChapterComment>();
}
