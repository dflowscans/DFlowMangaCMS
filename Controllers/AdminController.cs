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
        await EnsureDec30ChangelogAsync();

        return View(stats);
    }

    private async Task EnsureDec30ChangelogAsync()
    {
        var title = "The Big Update: XP, Replies & Customization";
        // Remove the old one if it exists to replace with comprehensive one
        var oldTitle = "Notifications & UI Enhancements";
        var oldEntry = await _context.ChangelogEntries.FirstOrDefaultAsync(e => e.Title == oldTitle || e.Title == title);
        
        var content = @"### 🌟 Social & Engagement
- **Comment XP System**: Earn XP for your activity! Longer comments earn more XP (up to 100 XP per comment). 
  *Note: XP is awarded for the first comment/reply per chapter. Self-replies do not count.*
- **TikTok-style Replies**: New arrow indicator showing exactly who you are replying to.
- **Reply Notifications**: Get notified instantly when someone replies to your comment.
- **Notification Tabs**: Organized notifications into **Series**, **Community**, and **System** categories.
- **Notification Management**: You can now mark individual notifications as read or delete them entirely.

### 🎨 UI & Customization
- **Banner Shadow Controls**: Full control over the featured banner description shadow (Strength & Depth) via the Admin panel.
- **Mobile Optimization**: Descriptions are now capped at 100 characters on mobile to keep the UI clean.
- **Social Media Cleanup**: Updated all links to our new Discord server [discord.gg/tyRD6Nn6Fr](https://discord.gg/tyRD6Nn6Fr) and removed legacy social icons.

### 🛠️ Improvements & Fixes
- **Nullable Chapter Titles**: Chapter titles are now optional! You can upload chapters with just a number.
- **Database Self-Healing**: Improved system stability and automatic database error resolution.
- **Real-time Leveling**: Your level and XP now update dynamically in the navigation bar.";

        if (oldEntry != null)
        {
            oldEntry.Title = title;
            oldEntry.Content = content;
            oldEntry.CreatedAt = new DateTime(2025, 12, 30, 12, 0, 0);
            _context.Update(oldEntry);
        }
        else
        {
            var entry = new ChangelogEntry
            {
                Title = title,
                Content = content,
                CreatedAt = new DateTime(2025, 12, 30, 12, 0, 0)
            };
            _context.ChangelogEntries.Add(entry);
        }
        await _context.SaveChangesAsync();
    }

    private async Task SeedChangelogsAsync()
    {
        var hasEntries = await _context.ChangelogEntries.AnyAsync();
        if (!hasEntries)
        {
            var seedDate = new DateTime(2025, 12, 28, 12, 0, 0); // Dec 28, 2025
            var entries = new List<ChangelogEntry>
            {
                new ChangelogEntry
                {
                    Title = "Social Media & Contact Updates",
                    Content = "### Updated Social Links\n- Updated Discord link to our new server: [discord.gg/tyRD6Nn6Fr](https://discord.gg/tyRD6Nn6Fr)\n- Removed outdated social media links (Twitter, GitHub).\n- Linked the **Contact** page directly to our Discord server for faster support.",
                    CreatedAt = seedDate
                },
                new ChangelogEntry
                {
                    Title = "Comment XP & TikTok Replies",
                    Content = "### New Features\n- **Comment XP System**: Earn XP for every comment you post! Longer comments (excluding markdown) earn more XP.\n- **TikTok-style Replies**: See exactly who is replying to whom with a clear arrow indicator after the username.\n- **Reply Notifications**: Get notified instantly when someone replies to your comment.\n- **XP Rules**: XP is awarded for your first comment and first reply per chapter. Self-replies do not count towards XP.",
                    CreatedAt = seedDate.AddMinutes(5)
                },
                new ChangelogEntry
                {
                    Title = "Chapter Improvements",
                    Content = "### Improvements\n- **Nullable Chapter Titles**: Chapter titles are now optional. You can upload chapters with just a number if they don't have a specific title.\n- **Database Stability**: Improved database self-healing logic to prevent errors during updates.",
                    CreatedAt = seedDate.AddMinutes(10)
                },
                new ChangelogEntry
                {
                    Title = "Site Changelog & Fixes",
                    Content = "### New Features\n- **Public Changelog**: Added this page to keep everyone updated on the latest site changes!\n- **Fixes**: Resolved a critical error with notification loading and improved overall site performance.",
                    CreatedAt = DateTime.UtcNow // Use current time for the changelog feature itself
                }
            };

            _context.ChangelogEntries.AddRange(entries);
            await _context.SaveChangesAsync();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSiteSettings(string key, string value)
    {
        if (!IsCurrentUserAdmin()) return Forbid();

        try
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SiteSetting { Key = key, Value = value };
                _context.SiteSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Settings updated successfully!";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update settings. The SiteSettings table might be missing. Please apply database fixes first.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    #region Manga Management

    // GET: Admin/MangaList
    public async Task<IActionResult> MangaList(string search = "")
    {
        var manga = _context.Mangas
            .Include(m => m.Chapters)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            manga = manga.Where(m => m.Title.Contains(search) || m.Author.Contains(search));
        }

        var result = await manga.OrderByDescending(m => m.CreatedAt).ToListAsync();
        ViewBag.Search = search;
        return View(result);
    }

    // GET: Admin/CreateManga
    public IActionResult CreateManga()
    {
        return View();
    }

    // POST: Admin/CreateManga
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManga([Bind("Title,Description,ImageUrl,BannerUrl,Author,Artist,Status,Type,Genre,Rating,IsFeatured,BannerPositionX,BannerPositionY,HasTitleShadow,TitleShadowSize,TitleShadowOpacity")] Manga manga)
    {
        if (ModelState.IsValid)
        {
            manga.CreatedAt = DateTime.UtcNow;
            manga.UpdatedAt = DateTime.UtcNow;
            _context.Add(manga);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MangaList));
        }

        return View(manga);
    }

    // GET: Admin/EditManga/5
    public async Task<IActionResult> EditManga(int id)
    {
        var manga = await _context.Mangas.FindAsync(id);
        if (manga == null)
        {
            return NotFound();
        }

        return View(manga);
    }

    // POST: Admin/EditManga/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditManga(int id, [Bind("Id,Title,Description,ImageUrl,BannerUrl,Author,Artist,Status,Type,Genre,Rating,IsFeatured,CreatedAt,BannerPositionX,BannerPositionY,HasTitleShadow,TitleShadowSize,TitleShadowOpacity")] Manga manga)
    {
        if (id != manga.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                manga.UpdatedAt = DateTime.UtcNow;
                _context.Update(manga);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MangaList));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await MangaExists(manga.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        return View(manga);
    }

    // GET: Admin/DeleteManga/5
    public async Task<IActionResult> DeleteManga(int id)
    {
        var manga = await _context.Mangas.FindAsync(id);
        if (manga == null)
        {
            return NotFound();
        }

        return View(manga);
    }

    // POST: Admin/DeleteManga/5
    [HttpPost, ActionName("DeleteManga")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMangaConfirmed(int id)
    {
        var manga = await _context.Mangas.FindAsync(id);
        if (manga != null)
        {
            _context.Mangas.Remove(manga);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(MangaList));
    }

    private async Task<bool> MangaExists(int id)
    {
        return await _context.Mangas.AnyAsync(e => e.Id == id);
    }

    #endregion

    #region Chapter Management

    // GET: Admin/ChapterList/5
    public async Task<IActionResult> ChapterList(int mangaId, string search = "")
    {
        var manga = await _context.Mangas.FindAsync(mangaId);
        if (manga == null)
        {
            return NotFound();
        }

        var chapters = _context.Chapters
            .Where(c => c.MangaId == mangaId)
            .Include(c => c.Pages)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            chapters = chapters.Where(c => c.Title.Contains(search) || c.ChapterNumber.ToString().Contains(search));
        }

        var result = await chapters.OrderByDescending(c => c.ChapterNumber).ToListAsync();
        
        ViewBag.Manga = manga;
        ViewBag.Search = search;
        ViewBag.MangaId = mangaId;
        
        return View(result);
    }

    // GET: Admin/CreateChapter/5
    public async Task<IActionResult> CreateChapter(int mangaId)
    {
        var manga = await _context.Mangas.FindAsync(mangaId);
        if (manga == null)
        {
            return NotFound();
        }

        var chapter = new Chapter 
        { 
            MangaId = mangaId,
            Title = null,
            Description = string.Empty,
            CoverImageUrl = string.Empty,
            Manga = manga
        };
        ViewBag.Manga = manga;
        return View(chapter);
    }

    // POST: Admin/CreateChapter
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChapter([Bind("MangaId,ChapterNumber,Title,Description,CoverImageUrl,ReleasedDate")] Chapter chapter, List<IFormFile> pages, string pageUrls)
    {
        if (ModelState.IsValid)
        {
            try 
            {
                await _chapterService.CreateChapterAsync(chapter, pages, pageUrls);
                return RedirectToAction(nameof(ChapterList), new { mangaId = chapter.MangaId });
            }
            catch (Exception)
            {
                // Log exception
                ModelState.AddModelError("", "An error occurred while creating the chapter.");
            }
        }

        var mangaForView = await _context.Mangas.FindAsync(chapter.MangaId);
        ViewBag.Manga = mangaForView;
        ViewBag.PageUrls = pageUrls;
        return View(chapter);
    }

    // GET: Admin/EditChapter/5
    public async Task<IActionResult> EditChapter(int id)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == id);
        if (chapter == null)
        {
            return NotFound();
        }

        ViewBag.Manga = chapter.Manga;
        return View(chapter);
    }

    // POST: Admin/EditChapter/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChapter(int id, [Bind("Id,MangaId,ChapterNumber,Title,Description,CoverImageUrl,ReleasedDate,ViewCount,CreatedAt")] Chapter chapter)
    {
        if (id != chapter.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                chapter.UpdatedAt = DateTime.UtcNow;
                _context.Update(chapter);
                
                var manga = await _context.Mangas.FindAsync(chapter.MangaId);
                if (manga != null)
                {
                    manga.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ChapterList), new { mangaId = chapter.MangaId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ChapterExists(chapter.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        var mangaForView = await _context.Mangas.FindAsync(chapter.MangaId);
        ViewBag.Manga = mangaForView;
        return View(chapter);
    }

    // GET: Admin/DeleteChapter/5
    public async Task<IActionResult> DeleteChapter(int id)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == id);
        if (chapter == null)
        {
            return NotFound();
        }

        return View(chapter);
    }

    // POST: Admin/DeleteChapter/5
    [HttpPost, ActionName("DeleteChapter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapterConfirmed(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter != null)
        {
            int mangaId = chapter.MangaId;
            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ChapterList), new { mangaId = mangaId });
        }

        return NotFound();
    }

    private async Task<bool> ChapterExists(int id)
    {
        return await _context.Chapters.AnyAsync(e => e.Id == id);
    }

    #endregion

    #region Chapter Pages Management

    // GET: Admin/PageList/5
    public async Task<IActionResult> PageList(int chapterId)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).Include(c => c.Pages).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null)
        {
            return NotFound();
        }

        var pages = await _context.ChapterPages
            .Where(p => p.ChapterId == chapterId)
            .OrderBy(p => p.PageNumber)
            .ToListAsync();

        ViewBag.Chapter = chapter;
        ViewBag.ChapterId = chapterId;
        return View(pages);
    }

    // GET: Admin/CreatePage/5
    public async Task<IActionResult> CreatePage(int chapterId)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null)
        {
            return NotFound();
        }

        var pageCount = await _context.ChapterPages.Where(p => p.ChapterId == chapterId).CountAsync();
        var page = new ChapterPage 
        { 
            ChapterId = chapterId, 
            PageNumber = pageCount + 1,
            ImageUrl = string.Empty,
            Chapter = chapter
        };
        
        ViewBag.Chapter = chapter;
        return View(page);
    }

    // POST: Admin/CreatePage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePage([Bind("ChapterId,PageNumber,ImageUrl,Width,Height")] ChapterPage page)
    {
        if (ModelState.IsValid)
        {
            page.CreatedAt = DateTime.UtcNow;
            _context.Add(page);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PageList), new { chapterId = page.ChapterId });
        }

        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == page.ChapterId);
        ViewBag.Chapter = chapter;
        return View(page);
    }

    // GET: Admin/EditPage/5
    public async Task<IActionResult> EditPage(int id)
    {
        var page = await _context.ChapterPages.Include(c => c.Chapter!).ThenInclude(c => c.Manga!).FirstOrDefaultAsync(p => p.Id == id);
        if (page == null)
        {
            return NotFound();
        }

        ViewBag.Chapter = page.Chapter;
        return View(page);
    }

    // POST: Admin/EditPage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPage(int id, [Bind("Id,ChapterId,PageNumber,ImageUrl,Width,Height,CreatedAt")] ChapterPage page)
    {
        if (id != page.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(page);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PageList), new { chapterId = page.ChapterId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PageExists(page.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == page.ChapterId);
        ViewBag.Chapter = chapter;
        return View(page);
    }

    // GET: Admin/DeletePage/5
    public async Task<IActionResult> DeletePage(int id)
    {
        var page = await _context.ChapterPages.Include(c => c.Chapter).FirstOrDefaultAsync(p => p.Id == id);
        if (page == null)
        {
            return NotFound();
        }

        return View(page);
    }

    // POST: Admin/DeletePage/5
    [HttpPost, ActionName("DeletePage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePageConfirmed(int id)
    {
        var page = await _context.ChapterPages.FindAsync(id);
        if (page != null)
        {
            int chapterId = page.ChapterId;
            _context.ChapterPages.Remove(page);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PageList), new { chapterId = chapterId });
        }

        return NotFound();
    }

    #region Changelog Management

    public async Task<IActionResult> ChangelogList()
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        var entries = await _context.ChangelogEntries.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(entries);
    }

    public IActionResult CreateChangelog()
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChangelog([Bind("Title,Content,CreatedAt")] ChangelogEntry entry, [FromServices] INotificationService notificationService)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        if (ModelState.IsValid)
        {
            if (entry.CreatedAt == default)
            {
                entry.CreatedAt = DateTime.UtcNow;
            }
            _context.Add(entry);
            await _context.SaveChangesAsync();

            // Notify followers
            var followers = await _context.Users.Where(u => u.FollowChangelog).ToListAsync();
            foreach (var user in followers)
            {
                await notificationService.CreateNotificationAsync(
                    user.Id,
                    NotificationType.System,
                    $"New Update: {entry.Title}"
                );
            }

            return RedirectToAction(nameof(ChangelogList));
        }
        return View(entry);
    }

    public async Task<IActionResult> EditChangelog(int id)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        var entry = await _context.ChangelogEntries.FindAsync(id);
        if (entry == null) return NotFound();
        return View(entry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChangelog(int id, [Bind("Id,Title,Content,CreatedAt")] ChangelogEntry entry)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        if (id != entry.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(entry);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ChangelogList));
        }
        return View(entry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChangelog(int id)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();
        var entry = await _context.ChangelogEntries.FindAsync(id);
        if (entry != null)
        {
            _context.ChangelogEntries.Remove(entry);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ChangelogList));
    }

    #endregion

    private async Task<bool> PageExists(int id)
    {
        return await _context.ChapterPages.AnyAsync(e => e.Id == id);
    }

    // GET: Admin/BulkCreatePages/5
    public async Task<IActionResult> BulkCreatePages(int chapterId)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null)
        {
            return NotFound();
        }

        ViewBag.Chapter = chapter;
        ViewBag.ChapterId = chapterId;
        return View();
    }

    // POST: Admin/BulkCreatePages
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkCreatePages(int chapterId, string imageUrls)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(imageUrls))
        {
            ModelState.AddModelError("", "Please provide at least one image URL.");
            ViewBag.Chapter = chapter;
            ViewBag.ChapterId = chapterId;
            return View();
        }

        // Get current page count
        var currentPageCount = await _context.ChapterPages.Where(p => p.ChapterId == chapterId).CountAsync();
        
        // Split by newlines and filter empty lines
        var urls = imageUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(u => u.Trim())
                             .Where(u => !string.IsNullOrWhiteSpace(u))
                             .ToList();

        if (urls.Count == 0)
        {
            ModelState.AddModelError("", "No valid URLs found.");
            ViewBag.Chapter = chapter;
            ViewBag.ChapterId = chapterId;
            return View();
        }

        // Create pages
        var pages = new List<ChapterPage>();
        for (int i = 0; i < urls.Count; i++)
        {
            pages.Add(new ChapterPage
            {
                ChapterId = chapterId,
                PageNumber = currentPageCount + i + 1,
                ImageUrl = urls[i],
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.ChapterPages.AddRange(pages);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Successfully added {pages.Count} pages!";
        return RedirectToAction(nameof(PageList), new { chapterId = chapterId });
    }

    #endregion

    #region User Management
    // GET: Admin/ManageUsers
    public async Task<IActionResult> ManageUsers(string search = "")
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search));
        }
        var users = await query
            .OrderBy(u => u.Username)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CanManage = IsCurrentUserAdmin();
        ViewBag.AvailableTitles = await _context.UserTitles.OrderBy(t => t.Name).ToListAsync();
        ViewBag.AvailableDecorations = await _context.PfpDecorations.OrderBy(d => d.Name).ToListAsync();
        
        return View(users);
    }

    // POST: Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string username, string password, bool isAdmin = false, bool isSubAdmin = false, bool isActive = true)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Username and password are required.";
            return RedirectToAction(nameof(ManageUsers));
        }

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            TempData["ErrorMessage"] = "Username already exists.";
            return RedirectToAction(nameof(ManageUsers));
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsAdmin = isAdmin,
            IsSubAdmin = isSubAdmin,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "User created successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // POST: Admin/UpdateRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int userId, bool isAdmin, bool isSubAdmin)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(ManageUsers));
        }

        user.IsAdmin = isAdmin;
        user.IsSubAdmin = isSubAdmin;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Roles updated.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // POST: Admin/ResetUserPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUserPassword(int userId, string newPassword)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["ErrorMessage"] = "New password cannot be empty.";
            return RedirectToAction(nameof(ManageUsers));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(ManageUsers));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Password for user '{user.Username}' has been reset successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // POST: Admin/ToggleActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int userId)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(ManageUsers));
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "User status updated.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // POST: Admin/AwardTitle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AwardTitle(int userId, int titleId)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();

        var user = await _context.Users.FindAsync(userId);
        var title = await _context.UserTitles.FindAsync(titleId);

        if (user == null || title == null)
        {
            TempData["ErrorMessage"] = "User or Title not found.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // Check if user already has this title
        var alreadyHas = await _context.Set<UserUnlockedTitle>()
            .AnyAsync(ut => ut.UserId == userId && ut.TitleId == titleId);

        if (!alreadyHas)
        {
            _context.Set<UserUnlockedTitle>().Add(new UserUnlockedTitle
            {
                UserId = userId,
                TitleId = titleId,
                UnlockedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Title '{title.Name}' awarded to {user.Username}.";
        }
        else
        {
            TempData["ErrorMessage"] = $"{user.Username} already has the title '{title.Name}'.";
        }

        return RedirectToAction(nameof(ManageUsers));
    }

    // POST: Admin/AwardDecoration
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AwardDecoration(int userId, int decorationId)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Forbid();

        var user = await _context.Users.FindAsync(userId);
        var decoration = await _context.PfpDecorations.FindAsync(decorationId);

        if (user == null || decoration == null)
        {
            TempData["ErrorMessage"] = "User or Decoration not found.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // Check if user already has this decoration
        var alreadyHas = await _context.Set<UserUnlockedDecoration>()
            .AnyAsync(ud => ud.UserId == userId && ud.DecorationId == decorationId);

        if (!alreadyHas)
        {
            _context.Set<UserUnlockedDecoration>().Add(new UserUnlockedDecoration
            {
                UserId = userId,
                DecorationId = decorationId,
                UnlockedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Decoration '{decoration.Name}' awarded to {user.Username}.";
        }
        else
        {
            TempData["ErrorMessage"] = $"{user.Username} already has the decoration '{decoration.Name}'.";
        }

        return RedirectToAction(nameof(ManageUsers));
    }
    #endregion

    #region Emoji Management
    // GET: Admin/ManageEmojis
    public IActionResult ManageEmojis()
    {
        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "emojis.json");
        var emojisJson = System.IO.File.ReadAllText(filePath);
        var emojis = JsonSerializer.Deserialize<List<EmojiModel>>(emojisJson) ?? new List<EmojiModel>();
        return View(emojis);
    }

    // POST: Admin/ManageEmojis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ManageEmojis(string emojisJson)
    {
        if (string.IsNullOrWhiteSpace(emojisJson))
        {
            return BadRequest("JSON cannot be empty");
        }

        try
        {
            // Validate JSON
            var emojis = JsonSerializer.Deserialize<List<EmojiModel>>(emojisJson);
            if (emojis == null) return BadRequest("Invalid JSON format");

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "emojis.json");
            System.IO.File.WriteAllText(filePath, emojisJson);
            TempData["SuccessMessage"] = "Emojis updated successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error updating emojis: " + ex.Message;
        }

        return RedirectToAction(nameof(ManageEmojis));
    }
    #endregion

    #region Decoration Management

    public async Task<IActionResult> ManageDecorations()
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        var decorations = await _context.PfpDecorations.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return View(decorations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDecoration(PfpDecoration decoration)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        if (ModelState.IsValid)
        {
            decoration.CreatedAt = DateTime.UtcNow;
            _context.PfpDecorations.Add(decoration);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageDecorations));
        }
        return View("ManageDecorations", await _context.PfpDecorations.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDecoration(PfpDecoration decoration)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        if (ModelState.IsValid)
        {
            var existing = await _context.PfpDecorations.FindAsync(decoration.Id);
            if (existing == null) return NotFound();

            existing.Name = decoration.Name;
            existing.ImageUrl = decoration.ImageUrl;
            existing.LevelRequirement = decoration.LevelRequirement;
            existing.IsAnimated = decoration.IsAnimated;
            existing.IsLocked = decoration.IsLocked;

            _context.Update(existing);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageDecorations));
        }
        return View("ManageDecorations", await _context.PfpDecorations.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDecoration(int id)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        var decoration = await _context.PfpDecorations.FindAsync(id);
        if (decoration != null)
        {
            _context.PfpDecorations.Remove(decoration);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageDecorations));
    }

    #endregion

    #region Title Management

    public async Task<IActionResult> ManageTitles()
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        var titles = await _context.UserTitles.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return View(titles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTitle(UserTitle title)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        if (ModelState.IsValid)
        {
            title.CreatedAt = DateTime.UtcNow;
            _context.UserTitles.Add(title);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageTitles));
        }
        return View("ManageTitles", await _context.UserTitles.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTitle(UserTitle title)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        if (ModelState.IsValid)
        {
            var existing = await _context.UserTitles.FindAsync(title.Id);
            if (existing == null) return NotFound();

            existing.Name = title.Name;
            existing.Color = title.Color;
            existing.LevelRequirement = title.LevelRequirement;
            existing.IsLocked = title.IsLocked;

            _context.Update(existing);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageTitles));
        }
        return View("ManageTitles", await _context.UserTitles.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTitle(int id)
    {
        if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin()) return Unauthorized();
        var title = await _context.UserTitles.FindAsync(id);
        if (title != null)
        {
            _context.UserTitles.Remove(title);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageTitles));
    }

    #endregion

    #region Database Fixes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FixDatabase()
    {
        if (!IsCurrentUserAdmin()) return Forbid();

        try
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                await _context.Database.OpenConnectionAsync();
                
                // 1. Fix ChapterComments table (ParentCommentId)
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'ParentCommentId' AND TABLE_SCHEMA = DATABASE();";
                var colResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(colResult) == 0)
                {
                    command.CommandText = "ALTER TABLE ChapterComments ADD COLUMN ParentCommentId INT NULL;";
                    await command.ExecuteNonQueryAsync();
                }

                // 2. Fix SiteSettings table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'SiteSettings' AND TABLE_SCHEMA = DATABASE();";
                var tableResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(tableResult) == 0)
                {
                    command.CommandText = @"
                        CREATE TABLE SiteSettings (
                            `Key` VARCHAR(100) NOT NULL,
                            `Value` LONGTEXT NOT NULL,
                            PRIMARY KEY (`Key`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                    await command.ExecuteNonQueryAsync();
                }

                // 3. Fix UserUnlockedDecoration table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'UserUnlockedDecoration' AND TABLE_SCHEMA = DATABASE();";
                var decTableResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(decTableResult) == 0)
                {
                    command.CommandText = @"
                        CREATE TABLE UserUnlockedDecoration (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            UserId INT NOT NULL,
                            DecorationId INT NOT NULL,
                            UnlockedAt DATETIME(6) NOT NULL,
                            CONSTRAINT FK_UserUnlockedDecoration_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_UserUnlockedDecoration_Decorations_DecId FOREIGN KEY (DecorationId) REFERENCES PfpDecorations(Id) ON DELETE CASCADE
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                    await command.ExecuteNonQueryAsync();
                }

                // 4. Fix UserUnlockedTitle table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'UserUnlockedTitle' AND TABLE_SCHEMA = DATABASE();";
                var titleTableResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(titleTableResult) == 0)
                {
                    command.CommandText = @"
                        CREATE TABLE UserUnlockedTitle (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            UserId INT NOT NULL,
                            TitleId INT NOT NULL,
                            UnlockedAt DATETIME(6) NOT NULL,
                            CONSTRAINT FK_UserUnlockedTitle_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_UserUnlockedTitle_Titles_TitleId FOREIGN KEY (TitleId) REFERENCES UserTitles(Id) ON DELETE CASCADE
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                    await command.ExecuteNonQueryAsync();
                }

                // 5. Fix Notifications table
                command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
                var notifyTableResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(notifyTableResult) == 0)
                {
                    command.CommandText = @"
                        CREATE TABLE Notifications (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            UserId INT NOT NULL,
                            Type INT NOT NULL,
                            Message VARCHAR(500) NOT NULL,
                            IsRead TINYINT(1) NOT NULL DEFAULT 0,
                            CreatedAt DATETIME(6) NOT NULL,
                            RelatedMangaId INT NULL,
                            RelatedChapterId INT NULL,
                            RelatedCommentId INT NULL,
                            TriggerUserId INT NULL,
                            CONSTRAINT FK_Notifications_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Notifications_Users_TriggerUserId FOREIGN KEY (TriggerUserId) REFERENCES Users(Id) ON DELETE SET NULL,
                            CONSTRAINT FK_Notifications_Mangas_MangaId FOREIGN KEY (RelatedMangaId) REFERENCES Mangas(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Notifications_Chapters_ChapterId FOREIGN KEY (RelatedChapterId) REFERENCES Chapters(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Notifications_ChapterComments_CommentId FOREIGN KEY (RelatedCommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                    await command.ExecuteNonQueryAsync();
                }

                // 6. Fix Mangas table (AniListId)
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
                var aniListColResult = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(aniListColResult) == 0)
                {
                    command.CommandText = "ALTER TABLE Mangas ADD COLUMN AniListId INT NULL;";
                    await command.ExecuteNonQueryAsync();
                }

                // 7. Fix Chapters table (Title nullable)
        command.CommandText = "ALTER TABLE Chapters MODIFY COLUMN Title VARCHAR(300) NULL;";
        await command.ExecuteNonQueryAsync();

        // 8. Add RepliedToUserId to ChapterComments
         try
         {
             command.CommandText = "ALTER TABLE ChapterComments ADD COLUMN RepliedToUserId INT NULL;";
             await command.ExecuteNonQueryAsync();
             command.CommandText = "ALTER TABLE ChapterComments ADD CONSTRAINT FK_ChapterComments_Users_RepliedToUserId FOREIGN KEY (RepliedToUserId) REFERENCES Users(Id);";
             await command.ExecuteNonQueryAsync();
         }
         catch { /* Column might already exist */ }

          // 9. Add ChangelogEntries table
          command.CommandText = @"
              CREATE TABLE IF NOT EXISTS ChangelogEntries (
                  Id INT AUTO_INCREMENT PRIMARY KEY,
                  Title VARCHAR(200) NOT NULL,
                  Content TEXT NOT NULL,
                  CreatedAt DATETIME NOT NULL
              );";
          await command.ExecuteNonQueryAsync();

          // 10. Add FollowChangelog to Users table
          try
          {
              command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
              var followColResult = await command.ExecuteScalarAsync();
              if (Convert.ToInt32(followColResult) == 0)
              {
                  command.CommandText = "ALTER TABLE Users ADD COLUMN FollowChangelog TINYINT(1) NOT NULL DEFAULT 1;";
                  await command.ExecuteNonQueryAsync();
              }
          }
          catch { /* Might already exist */ }

                TempData["SuccessMessage"] = "Database fixes applied successfully!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error updating database: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
    #endregion
}

public class EmojiModel
{
    public string code { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public int size { get; set; } = 24;
}
