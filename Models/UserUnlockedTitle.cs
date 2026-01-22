using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public enum UnlockOrigin
{
    AdminAward,
    LevelUnlock
}

public class UserUnlockedTitle
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TitleId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    public UnlockOrigin Origin { get; set; } = UnlockOrigin.LevelUnlock;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("TitleId")]
    public virtual UserTitle? Title { get; set; }
}
