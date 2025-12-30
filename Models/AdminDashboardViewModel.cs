namespace MangaReader.Models;

public class AdminDashboardViewModel
{
    public int TotalManga { get; set; }
    public int TotalChapters { get; set; }
    public int TotalPages { get; set; }
    public int TotalViews { get; set; }
    public List<Manga> RecentManga { get; set; } = new();
    public List<Chapter> RecentChapters { get; set; } = new();
}
