using MangaReader.Models;

namespace MangaReader.Services;

public interface IBookmarkService
{
    Task<IEnumerable<UserBookmark>> GetUserBookmarksAsync(int userId);
    Task<bool> AddOrUpdateBookmarkAsync(int userId, int mangaId, BookmarkStatus status);
    Task<bool> RemoveBookmarkAsync(int userId, int mangaId);
    Task<UserBookmark?> GetBookmarkAsync(int userId, int mangaId);
}
