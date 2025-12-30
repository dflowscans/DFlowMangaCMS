using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3)]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 6)]
    public required string PasswordHash { get; set; }

    public bool IsAdmin { get; set; } = false;

    public bool IsSubAdmin { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<UserBookmark> Bookmarks { get; set; } = [];
    public ICollection<ChapterComment> Comments { get; set; } = [];
    public virtual ICollection<Notification> Notifications { get; set; } = [];
    public virtual ICollection<CommentReaction> CommentReactions { get; set; } = [];

    // Unlocked Items
    public virtual ICollection<UserUnlockedDecoration> UnlockedDecorations { get; set; } = [];
    public virtual ICollection<UserUnlockedTitle> UnlockedTitles { get; set; } = [];

    // Profile
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    // Leveling System
    public int XP { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int? EquippedDecorationId { get; set; }

    [ForeignKey("EquippedDecorationId")]
    public virtual PfpDecoration? EquippedDecoration { get; set; }

    public int? EquippedTitleId { get; set; }

    [ForeignKey("EquippedTitleId")]
    public virtual UserTitle? EquippedTitle { get; set; }

    // Settings
    public bool HideReadingList { get; set; } = false;
    public bool FollowChangelog { get; set; } = true;
}
