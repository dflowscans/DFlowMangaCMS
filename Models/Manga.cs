using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class Manga
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200)]
    public required string Title { get; set; }

    [StringLength(1000)]
    public required string Description { get; set; }

    [StringLength(500)]
    public required string ImageUrl { get; set; }

    [StringLength(500)]
    public required string BannerUrl { get; set; }

    [StringLength(200)]
    public required string Author { get; set; }

    [StringLength(200)]
    public required string Artist { get; set; }

    [StringLength(500)]
    public required string Status { get; set; } // Ongoing, Completed, Hiatus

    [StringLength(100)]
    public string? Type { get; set; }

    [StringLength(500)]
    public required string Genre { get; set; } // Comma-separated genres

    public int? Rating { get; set; } // 1-10 rating

    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastChapterDate { get; set; }

    public int BannerPositionX { get; set; } = 50;
    public int BannerPositionY { get; set; } = 50;

    public bool HasTitleShadow { get; set; }
    public int TitleShadowSize { get; set; } = 12;
    [Range(0,2)]
    public double TitleShadowOpacity { get; set; } = 0.8;

    public int? AniListId { get; set; }

    // Navigation
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
