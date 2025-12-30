using MangaReader.Data;
using MangaReader.Models;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Services;

public class BookmarkService : IBookmarkService
{
    private readonly ApplicationDbContext _context;

    public BookmarkService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserBookmark>> GetUserBookmarksAsync(int userId)
    {
        return await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddOrUpdateBookmarkAsync(int userId, int mangaId, BookmarkStatus status)
    {
        var manga = await _context.Mangas.FindAsync(mangaId);
        if (manga == null)
        {
            return false;
        }

        var existingBookmark = await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);

        if (existingBookmark != null)
        {
            existingBookmark.Status = status;
            existingBookmark.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var bookmark = new UserBookmark
            {
                UserId = userId,
                MangaId = mangaId,
                Status = status,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserBookmarks.Add(bookmark);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(int userId, int mangaId)
    {
        var bookmark = await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);

        if (bookmark != null)
        {
            _context.UserBookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<UserBookmark?> GetBookmarkAsync(int userId, int mangaId)
    {
        return await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);
    }
}
