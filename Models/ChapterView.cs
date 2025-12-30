using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class ChapterView
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ChapterId { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("ChapterId")]
    public virtual Chapter? Chapter { get; set; }
}
