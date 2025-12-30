using MangaReader.Data;
using MangaReader.Models;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Services;

public class ChapterService : IChapterService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly INotificationService _notificationService;

    public ChapterService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, INotificationService notificationService)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _notificationService = notificationService;
    }

    public async Task CreateChapterAsync(Chapter chapter, List<IFormFile> pages, string pageUrls)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                chapter.CreatedAt = DateTime.UtcNow;
                chapter.UpdatedAt = DateTime.UtcNow;
                chapter.ViewCount = 0;

            var manga = await _context.Mangas.FindAsync(chapter.MangaId);
            if (manga != null)
            {
                manga.UpdatedAt = DateTime.UtcNow;
                manga.LastChapterDate = DateTime.UtcNow;
            }

            _context.Add(chapter);
            await _context.SaveChangesAsync();

            // Create notifications for bookmarked users
            if (manga != null)
            {
                var bookmarkedUserIds = await _context.UserBookmarks
                    .Where(b => b.MangaId == manga.Id)
                    .Select(b => b.UserId)
                    .ToListAsync();

                foreach (var userId in bookmarkedUserIds)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId,
                        NotificationType.Comic,
                        $"New chapter released: {manga.Title} - Ch. {chapter.ChapterNumber}",
                        mangaId: manga.Id,
                        chapterId: chapter.Id
                    );
                }
            }

            // Handle Page Uploads (Files)
            int pageNumber = 1;
            var newPages = new List<ChapterPage>();

            if (pages != null && pages.Count > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "manga", chapter.MangaId.ToString(), chapter.Id.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var file in pages)
                {
                    if (file.Length > 0)
                    {
                        // Validate File Type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue; // Skip invalid files
                        }

                        // Validate File Size (max 10MB)
                        if (file.Length > 10 * 1024 * 1024)
                        {
                            continue; // Skip too large
                        }

                        string uniqueFileName = $"{pageNumber:000}_{Guid.NewGuid()}{extension}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        newPages.Add(new ChapterPage
                        {
                            ChapterId = chapter.Id,
                            PageNumber = pageNumber++,
                            ImageUrl = $"/uploads/manga/{chapter.MangaId}/{chapter.Id}/{uniqueFileName}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Handle Page URLs (Direct Links)
            if (!string.IsNullOrWhiteSpace(pageUrls))
            {
                var urls = pageUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(u => u.Trim())
                                   .Where(u => !string.IsNullOrWhiteSpace(u));

                foreach (var url in urls)
                {
                    newPages.Add(new ChapterPage
                    {
                        ChapterId = chapter.Id,
                        PageNumber = pageNumber++,
                        ImageUrl = url,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (newPages.Any())
            {
                _context.ChapterPages.AddRange(newPages);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        });
    }
}
