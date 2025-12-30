using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class UserUnlockedDecoration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DecorationId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("DecorationId")]
    public virtual PfpDecoration? Decoration { get; set; }
}
