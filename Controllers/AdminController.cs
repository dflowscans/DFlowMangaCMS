using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MangaReader.Data;
using MangaReader.Models;
using MangaReader.Services;
using System.IO;
using System.Text.Json;
using System.Data;

namespace MangaReader.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IChapterService _chapterService;
    
    private bool IsCurrentUserAdmin()
    {
        return (User?.FindFirst("IsAdmin")?.Value == "True");
    }
    
    private bool IsCurrentUserSubAdmin()
    {
        return (User?.FindFirst("IsSubAdmin")?.Value == "True");
    }

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IWebHostEnvironment webHostEnvironment, IChapterService chapterService)
    {
        _context = context;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _chapterService = chapterService;
    }

    // Dashboard
    public async Task<IActionResult> Index()
    {
        var stats = new AdminDashboardViewModel
        {
            TotalManga = await _context.Mangas.CountAsync(),
            TotalChapters = await _context.Chapters.CountAsync(),
            TotalPages = await _context.ChapterPages.CountAsync(),
            TotalViews = await _context.Chapters.SumAsync(c => c.ViewCount),
            RecentManga = await _context.Mangas.OrderByDescending(m => m.CreatedAt).Take(5).ToListAsync(),
            RecentChapters = await _context.Chapters.Include(c => c.Manga).OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync()
        };

        // Check if database fix is needed
        try
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                await _context.Database.OpenConnectionAsync();
                
                // Check ParentCommentId column
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'ParentCommentId' AND TABLE_SCHEMA = DATABASE();";
                var colResult = await command.ExecuteScalarAsync();
                var colMissing = Convert.ToInt32(colResult) == 0;

                // Check SiteSettings table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'SiteSettings' AND TABLE_SCHEMA = DATABASE();";
                var tableResult = await command.ExecuteScalarAsync();
                var tableMissing = Convert.ToInt32(tableResult) == 0;

                // Check Notifications table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
                var notifyResult = await command.ExecuteScalarAsync();
                var notifyMissing = Convert.ToInt32(notifyResult) == 0;

                // Check AniListId column in Mangas table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
                var aniListColResult = await command.ExecuteScalarAsync();
                var aniListColMissing = Convert.ToInt32(aniListColResult) == 0;

                // Check FollowChangelog column in Users table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
                var followChangelogColResult = await command.ExecuteScalarAsync();
                var followChangelogColMissing = Convert.ToInt32(followChangelogColResult) == 0;

                ViewBag.DatabaseFixNeeded = colMissing || tableMissing || notifyMissing || aniListColMissing || followChangelogColMissing;
                ViewBag.SiteSettingsTableMissing = tableMissing;
                ViewBag.ParentCommentIdMissing = colMissing;
                ViewBag.NotificationsTableMissing = notifyMissing;
                ViewBag.AniListIdMissing = aniListColMissing;
                ViewBag.FollowChangelogMissing = followChangelogColMissing;
            }
        }
        catch
        {
            ViewBag.DatabaseFixNeeded = false;
        }

        // Load site settings with fallback for missing table
        try 
        {
            ViewBag.EnableDecorations = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableDecorations"))?.Value ?? "true";
            ViewBag.EnableTitles = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableTitles"))?.Value ?? "true";
            ViewBag.EnableBannerShadow = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableBannerShadow"))?.Value ?? "false";
            ViewBag.BannerShadowStrength = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowStrength"))?.Value ?? "0.8";
            ViewBag.BannerShadowDepth = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowDepth"))?.Value ?? "4";
        }
        catch
        {
            ViewBag.EnableDecorations = "true";
            ViewBag.EnableTitles = "true";
            ViewBag.EnableBannerShadow = "false";
            ViewBag.BannerShadowStrength = "0.8";
            ViewBag.BannerShadowDepth = "4";
        }

        // Auto-generate previous changelogs if they don't exist
        await SeedChangelogsAsync();
        await EnsureInitialChangelogAsync();

        return View(stats);
    }

    private async Task EnsureInitialChangelogAsync()
    {
        var title = "Welcome to your new Manga Reader!";
        var entry = await _context.ChangelogEntries.FirstOrDefaultAsync(e => e.Title == title);
        
        var content = @"### 🌟 Getting Started
- **Admin Panel**: You can manage your manga, chapters, and site settings from here.
- **Customization**: Enable or disable features like XP, Titles, and Decorations in the settings.
- **Social**: Link your own Discord or social media in the layout file.

### 🛠️ Key Features
- **XP System**: Users earn XP for reading and commenting.
- **Notifications**: Stay updated on new chapters and replies.
- **Responsive**: Works great on mobile and desktop.";

        if (entry == null)
        {
            entry = new ChangelogEntry
            {
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChangelogEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedChangelogsAsync()
    {
        // This method can be used to seed initial history if needed.
        // For a public version, we keep it simple.
    }
}
