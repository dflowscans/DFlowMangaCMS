using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class UserRating
{
    public int Id { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public int MangaId { get; set; }
    [Range(1,5)]
    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Manga? Manga { get; set; }
}
