using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public enum BookmarkStatus
{
    PlanToRead = 0,
    Reading = 1,
    Completed = 2,
    OnHold = 3,
    Dropped = 4
}

public class UserBookmark
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MangaId { get; set; }

    public BookmarkStatus Status { get; set; } = BookmarkStatus.PlanToRead;

    public DateTime AddedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("MangaId")]
    public Manga? Manga { get; set; }
}
