using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class CommentReaction
{
    public int Id { get; set; }

    public int CommentId { get; set; }

    public int UserId { get; set; }

    public bool IsLike { get; set; } // true = Like, false = Dislike

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CommentId")]
    public virtual ChapterComment? Comment { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
