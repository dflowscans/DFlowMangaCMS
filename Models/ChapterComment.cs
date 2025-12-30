using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class ChapterComment
{
    public int Id { get; set; }

    public int ChapterId { get; set; }

    public int UserId { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ParentCommentId { get; set; }

    public int? RepliedToUserId { get; set; }

    [ForeignKey("RepliedToUserId")]
    public User? RepliedToUser { get; set; }

    [ForeignKey("ParentCommentId")]
    public ChapterComment? ParentComment { get; set; }

    public ICollection<ChapterComment> Replies { get; set; } = new List<ChapterComment>();

    public virtual ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();

    [ForeignKey("ChapterId")]
    public Chapter? Chapter { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }
}
