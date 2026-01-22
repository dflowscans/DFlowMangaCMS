namespace MangaReader.Models;

public class AdminDashboardViewModel
{
    public int TotalManga { get; set; }
    public int TotalChapters { get; set; }
    public int TotalPages { get; set; }
    public int TotalViews { get; set; }
    public int TotalUsers { get; set; }
    public int UniqueVisitors { get; set; }
    public int NewUsersLast7Days { get; set; }
    public double ActiveRate { get; set; }
    public double UserEngagement { get; set; }

    public List<Manga> RecentManga { get; set; } = new();
    public List<Chapter> RecentChapters { get; set; } = new();
    
    // Chart Data
    public List<string> TrafficLabels { get; set; } = new();
    public List<int> TrafficViews { get; set; } = new();
    public List<int> TrafficVisitors { get; set; } = new();
    
    public List<string> PopularMangaLabels { get; set; } = new();
    public List<int> PopularMangaViews { get; set; } = new();

    public List<PopularChapterViewModel> PopularChapters { get; set; } = new();

    public List<string> GenreLabels { get; set; } = new();
    public List<int> GenreCounts { get; set; } = new();

    public List<string> DeviceLabels { get; set; } = new();
    public List<int> DeviceCounts { get; set; } = new();
}

public class PopularChapterViewModel
{
    public string MangaTitle { get; set; } = "";
    public double ChapterNumber { get; set; }
    public int Views { get; set; }
}
