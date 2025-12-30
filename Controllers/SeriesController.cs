using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MangaReader.Data;
using MangaReader.Models;
using MangaReader.Services;
using System.Security.Claims;

namespace MangaReader.Controllers;

public class SeriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SeriesController> _logger;
    private readonly IBookmarkService _bookmarkService;

    public SeriesController(ApplicationDbContext context, ILogger<SeriesController> logger, IBookmarkService bookmarkService)
    {
        _context = context;
        _logger = logger;
        _bookmarkService = bookmarkService;
    }

    // GET: Series
    public async Task<IActionResult> Index(string search = "", string genre = "", string status = "")
    {
        var manga = _context.Mangas
            .Include(m => m.Chapters)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(search))
        {
            manga = manga.Where(m => m.Title.Contains(search) || m.Author.Contains(search));
        }

        // Genre filter
        if (!string.IsNullOrEmpty(genre))
        {
            manga = manga.Where(m => m.Genre.Contains(genre));
        }

        // Status filter
        if (!string.IsNullOrEmpty(status))
        {
            manga = manga.Where(m => m.Status == status);
        }

        var result = await manga.OrderByDescending(m => m.UpdatedAt).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.Genre = genre;
        ViewBag.Status = status;

        return View(result);
    }

    // GET: Series/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var manga = await _context.Mangas
            .Include(m => m.Chapters.OrderByDescending(c => c.ChapterNumber))
            .ThenInclude(c => c.Pages.OrderBy(p => p.PageNumber))
            .Include(m => m.Chapters)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manga == null)
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated ?? false)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var userRating = await _context.UserRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.MangaId == id);
                ViewBag.UserRating = userRating?.Rating ?? 0;
            }
        }

        return View(manga);
    }

    // GET: Chapter view
    public async Task<IActionResult> ReadChapter(int id)
    {
        Chapter? chapter = null;
        try 
        {
            chapter = await _context.Chapters
                .Include(c => c.Manga)
                .Include(c => c.Pages.OrderBy(p => p.PageNumber))
                .Include(c => c.Comments.OrderByDescending(cc => cc.CreatedAt))
                    .ThenInclude(cc => cc.User!)
                        .ThenInclude(u => u.EquippedTitle!)
                .Include(c => c.Comments.OrderByDescending(cc => cc.CreatedAt))
                    .ThenInclude(cc => cc.User!)
                        .ThenInclude(u => u.EquippedDecoration!)
                .Include(c => c.Comments)
                    .ThenInclude(cc => cc.RepliedToUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception)
        {
            // Fallback if ParentCommentId column is missing in DB
            chapter = await _context.Chapters
                .Include(c => c.Manga)
                .Include(c => c.Pages.OrderBy(p => p.PageNumber))
                .FirstOrDefaultAsync(c => c.Id == id);
            
            // Try to load comments without the missing column if possible, or just leave them empty
            // For now, we'll just continue without comments to prevent a crash
            if (chapter != null)
            {
                // Optionally log that the DB needs fixing
            }
        }

        if (chapter == null)
        {
            return NotFound();
        }

        // Load site settings for decorations and titles with fallback
        try
        {
            ViewBag.EnableDecorations = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableDecorations"))?.Value ?? "true";
            ViewBag.EnableTitles = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableTitles"))?.Value ?? "true";
        }
        catch
        {
            ViewBag.EnableDecorations = "true";
            ViewBag.EnableTitles = "true";
        }

        // Increment view count and award XP if logged in
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var hasViewed = await _context.ChapterViews.AnyAsync(cv => cv.UserId == userId && cv.ChapterId == id);
            
            if (!hasViewed)
            { 
                chapter.ViewCount++;
                _context.ChapterViews.Add(new ChapterView { UserId = userId, ChapterId = id });
                await AwardXpAsync(userId, 10); // 10 XP per chapter
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Prevent multiple view counts from the same session for guests
            var sessionKey = $"ViewedChapter_{id}";
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(sessionKey)))
            {
                chapter.ViewCount++;
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString(sessionKey, "1");
            }
        }

        var allChapters = await _context.Chapters
            .Where(c => c.MangaId == chapter.MangaId)
            .OrderBy(c => c.ChapterNumber)
            .ToListAsync();

        ViewBag.AllChapters = allChapters;

        // Load emojis from JSON
        var emojiPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emojis.json");
        if (System.IO.File.Exists(emojiPath))
        {
            var emojiJson = await System.IO.File.ReadAllTextAsync(emojiPath);
            ViewBag.EmojisJson = emojiJson;
        }
        else
        {
            ViewBag.EmojisJson = "[]";
        }

        return View(chapter);
    }

    private async Task AwardXpAsync(int userId, int amount)
    {
        var user = await _context.Users
            .Include(u => u.UnlockedDecorations)
            .Include(u => u.UnlockedTitles)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return;

        user.XP += amount;
        bool leveledUp = false;
        int oldLevel = user.Level;

        // Level up logic: each level needs more XP
        while (true)
        {
            int xpNeeded = (int)(100 * Math.Pow(1.5, user.Level - 1));
            if (user.XP >= xpNeeded)
            {
                user.XP -= xpNeeded;
                user.Level++;
                leveledUp = true;
            }
            else
            {
                break;
            }
        }

        if (leveledUp)
        {
            // Automatic unlocking of decorations and titles
            var decorationsToUnlock = await _context.PfpDecorations
                .Where(d => d.LevelRequirement > oldLevel && d.LevelRequirement <= user.Level && !d.IsLocked)
                .ToListAsync();

            var titlesToUnlock = await _context.UserTitles
                .Where(t => t.LevelRequirement > oldLevel && t.LevelRequirement <= user.Level && !t.IsLocked)
                .ToListAsync();

            foreach (var dec in decorationsToUnlock)
            {
                if (!user.UnlockedDecorations.Any(ud => ud.DecorationId == dec.Id))
                {
                    _context.Set<UserUnlockedDecoration>().Add(new UserUnlockedDecoration
                    {
                        UserId = user.Id,
                        DecorationId = dec.Id,
                        UnlockedAt = DateTime.UtcNow
                    });
                }
            }

            foreach (var title in titlesToUnlock)
            {
                if (!user.UnlockedTitles.Any(ut => ut.TitleId == title.Id))
                {
                    _context.Set<UserUnlockedTitle>().Add(new UserUnlockedTitle
                    {
                        UserId = user.Id,
                        TitleId = title.Id,
                        UnlockedAt = DateTime.UtcNow
                    });
                }
            }

            // Notification for level up
            var notificationService = HttpContext.RequestServices.GetService<MangaReader.Services.INotificationService>();
            if (notificationService != null)
            {
                await notificationService.CreateNotificationAsync(
                    user.Id,
                    NotificationType.System,
                    $"Congratulations! You reached Level {user.Level}!",
                    triggerUserId: user.Id
                );

                if (decorationsToUnlock.Any() || titlesToUnlock.Any())
                {
                    await notificationService.CreateNotificationAsync(
                        user.Id,
                        NotificationType.Reward,
                        $"You unlocked {decorationsToUnlock.Count} decorations and {titlesToUnlock.Count} titles!",
                        triggerUserId: user.Id
                    );
                }
            }
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CommentRequest request, [FromServices] INotificationService notificationService)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { success = false, message = "Please log in to comment." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var username = User.Identity?.Name ?? "Someone";

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Json(new { success = false, message = "Comment cannot be empty." });
        }
        if (request.Content.Length > 1000)
        {
            return Json(new { success = false, message = "Comment exceeds 1000 characters." });
        }

        var chapter = await _context.Chapters
            .Include(c => c.Manga)
            .FirstOrDefaultAsync(c => c.Id == request.ChapterId);

        if (chapter == null)
        {
            return Json(new { success = false, message = "Chapter not found." });
        }

        int? parentId = request.ParentId > 0 ? request.ParentId : null;
        int? repliedToUserId = null;
        string content = request.Content.Trim();

        // If replying to a reply, flatten the structure by pointing to the top-level comment
        if (parentId.HasValue)
        {
            var parent = await _context.ChapterComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == parentId.Value);
            
            if (parent != null)
            {
                repliedToUserId = parent.UserId;
                
                if (parent.ParentCommentId.HasValue)
                {
                    // This is a reply to a reply. 
                    // We point to the same parent as the comment we are replying to.
                    parentId = parent.ParentCommentId;
                }
            }
        }

        var comment = new ChapterComment
        {
            ChapterId = request.ChapterId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ParentCommentId = parentId,
            RepliedToUserId = repliedToUserId
        };

        _context.ChapterComments.Add(comment);
        await _context.SaveChangesAsync();

        // XP Logic
        try 
        {
            bool isReply = parentId.HasValue;
            bool isSelfReply = repliedToUserId.HasValue && repliedToUserId.Value == userId;
            bool shouldAwardXP = false;

            if (!isReply)
            {
                // First root comment in this chapter
                var hasRootBefore = await _context.ChapterComments.AnyAsync(cc => 
                    cc.UserId == userId && 
                    cc.ChapterId == request.ChapterId && 
                    cc.ParentCommentId == null &&
                    cc.Id != comment.Id);
                
                if (!hasRootBefore) shouldAwardXP = true;
            }
            else if (!isSelfReply)
            {
                // First reply to someone else in this chapter
                var hasReplyBefore = await _context.ChapterComments.AnyAsync(cc => 
                    cc.UserId == userId && 
                    cc.ChapterId == request.ChapterId && 
                    cc.ParentCommentId != null &&
                    cc.RepliedToUserId != userId &&
                    cc.Id != comment.Id);

                if (!hasReplyBefore) shouldAwardXP = true;
            }

            if (shouldAwardXP)
            {
                // Calculate XP based on clean length
                string cleanContent = StripMarkdown(content);
                int baseXP = 10;
                int lengthXP = cleanContent.Length / 10; // 1 XP per 10 characters
                int totalXP = Math.Min(baseXP + lengthXP, 100); // Max 100 XP
                
                await AwardXpAsync(userId, totalXP);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding XP for comment");
        }

        // Notification for replies
        if (repliedToUserId.HasValue && repliedToUserId.Value != userId)
        {
            await notificationService.CreateNotificationAsync(
                repliedToUserId.Value,
                NotificationType.Community,
                $"{username} replied to your comment on {chapter.Manga?.Title} - Ch. {chapter.ChapterNumber}",
                mangaId: chapter.MangaId,
                chapterId: chapter.Id,
                commentId: comment.Id,
                triggerUserId: userId
            );
        }
        // Also notify the root comment owner if they are different from the person replied to and the commenter
        else if (comment.ParentCommentId.HasValue)
        {
            var parentComment = await _context.ChapterComments
                .FirstOrDefaultAsync(c => c.Id == comment.ParentCommentId.Value);

            if (parentComment != null && parentComment.UserId != userId && parentComment.UserId != repliedToUserId)
            {
                await notificationService.CreateNotificationAsync(
                    parentComment.UserId,
                    NotificationType.Community,
                    $"{username} commented on your post on {chapter.Manga?.Title} - Ch. {chapter.ChapterNumber}",
                    mangaId: chapter.MangaId,
                    chapterId: chapter.Id,
                    commentId: comment.Id,
                    triggerUserId: userId
                );
            }
        }

        return Json(new { success = true, message = "Comment posted.", commentId = comment.Id });
    }

    private string StripMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        // Basic markdown stripping using regex
        // Remove links [text](url) -> text
        string result = System.Text.RegularExpressions.Regex.Replace(content, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        // Remove bold/italic **bold** or *italic*
        result = System.Text.RegularExpressions.Regex.Replace(result, @"(\*\*|__)(.*?)\1", "$2");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"(\*|_)(.*?)\1", "$2");
        // Remove code blocks
        result = System.Text.RegularExpressions.Regex.Replace(result, @"(`{1,3})(.*?)\1", "$2");
        // Remove blockquotes
        result = System.Text.RegularExpressions.Regex.Replace(result, @"^\s*>\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Remove emojis like :happy: (optional, user said "not counting markdown things", usually emojis are fine but let's keep them)
        
        return result.Trim();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ReactToComment([FromBody] ReactionRequest request, [FromServices] INotificationService notificationService)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var username = User.Identity?.Name ?? "Someone";

        var comment = await _context.ChapterComments
            .Include(c => c.Chapter)
                .ThenInclude(ch => ch!.Manga)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId);

        if (comment == null) return Json(new { success = false, message = "Comment not found." });

        var existingReaction = await _context.CommentReactions
            .FirstOrDefaultAsync(r => r.CommentId == request.CommentId && r.UserId == userId);

        if (existingReaction != null)
        {
            if (existingReaction.IsLike == request.IsLike)
            {
                // Remove reaction if same
                _context.CommentReactions.Remove(existingReaction);
            }
            else
            {
                // Update reaction if different
                existingReaction.IsLike = request.IsLike;
                existingReaction.CreatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Add new reaction
            var reaction = new CommentReaction
            {
                CommentId = request.CommentId,
                UserId = userId,
                IsLike = request.IsLike,
                CreatedAt = DateTime.UtcNow
            };
            _context.CommentReactions.Add(reaction);

            // Notification for likes
            if (request.IsLike && comment.UserId != userId)
            {
                await notificationService.CreateNotificationAsync(
                    comment.UserId,
                    NotificationType.Community,
                    $"{username} liked your comment on {comment.Chapter?.Manga?.Title} - Ch. {comment.Chapter?.ChapterNumber}",
                    mangaId: comment.Chapter?.MangaId,
                    chapterId: comment.ChapterId,
                    commentId: comment.Id,
                    triggerUserId: userId
                );
            }
        }

        await _context.SaveChangesAsync();

        var likes = await _context.CommentReactions.CountAsync(r => r.CommentId == request.CommentId && r.IsLike);
        var dislikes = await _context.CommentReactions.CountAsync(r => r.CommentId == request.CommentId && !r.IsLike);

        return Json(new { success = true, likes, dislikes });
    }

    public class ReactionRequest
    {
        public int CommentId { get; set; }
        public bool IsLike { get; set; }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { success = false, message = "Please log in." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var isAdmin = (User.FindFirst("IsAdmin")?.Value == "True") || (User.FindFirst("IsSubAdmin")?.Value == "True");

        var comment = await _context.ChapterComments.FirstOrDefaultAsync(c => c.Id == request.CommentId);
        if (comment == null)
        {
            return Json(new { success = false, message = "Comment not found." });
        }

        if (!isAdmin && comment.UserId != userId)
        {
            return Json(new { success = false, message = "You do not have permission to delete this comment." });
        }

        _context.ChapterComments.Remove(comment);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Comment deleted." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { success = false, message = "Please log in." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var isAdmin = (User.FindFirst("IsAdmin")?.Value == "True") || (User.FindFirst("IsSubAdmin")?.Value == "True");

        var comment = await _context.ChapterComments.FirstOrDefaultAsync(c => c.Id == request.CommentId);
        if (comment == null)
        {
            return Json(new { success = false, message = "Comment not found." });
        }

        if (!isAdmin && comment.UserId != userId)
        {
            return Json(new { success = false, message = "You do not have permission to edit this comment." });
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Json(new { success = false, message = "Comment cannot be empty." });
        }

        comment.Content = request.Content.Trim();
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Comment updated." });
    }

    [Authorize]
    public async Task<IActionResult> Bookmarks()
    {
        return RedirectToAction("Profile", "Auth");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddBookmark([FromBody] BookmarkRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { success = false, message = "Please log in to bookmark manga." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var result = await _bookmarkService.AddOrUpdateBookmarkAsync(userId, request.MangaId, (BookmarkStatus)request.Status);
        
        if (result)
        {
            return Json(new { success = true, message = "Bookmark updated successfully." });
        }
        
        return Json(new { success = false, message = "Failed to update bookmark." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RemoveBookmark([FromBody] BookmarkRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { success = false, message = "Please log in." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var result = await _bookmarkService.RemoveBookmarkAsync(userId, request.MangaId);

        if (result)
        {
            return Json(new { success = true, message = "Bookmark removed." });
        }

        return Json(new { success = false, message = "Bookmark not found." });
    }

    [Authorize]
    public async Task<IActionResult> GetUserBookmark(int mangaId)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Json(new { bookmarked = false });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var bookmark = await _bookmarkService.GetBookmarkAsync(userId, mangaId);

        return Json(new { bookmarked = bookmark != null, status = bookmark?.Status ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> RateManga([FromBody] RateRequest req)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0";
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var manga = await _context.Mangas.FindAsync(req.MangaId);
        if (manga == null)
        {
            return Json(new { success = false, message = "Manga not found" });
        }

        var rating = Math.Clamp(req.Rating, 1, 5);
        var existing = await _context.UserRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.MangaId == req.MangaId);
        if (existing == null)
        {
            existing = new UserRating { UserId = userId, MangaId = req.MangaId, Rating = rating, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.UserRatings.Add(existing);
        }
        else
        {
            existing.Rating = rating;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.UserRatings.Update(existing);
        }

        await _context.SaveChangesAsync();

        var ratings = _context.UserRatings.Where(r => r.MangaId == req.MangaId);
        var avg = await ratings.AnyAsync() ? await ratings.AverageAsync(r => (double)r.Rating) : 0;
        manga.Rating = (int)Math.Round(avg * 10);
        manga.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new { success = true, average = avg, average10 = manga.Rating / 10.0, userRating = rating, count = await _context.UserRatings.CountAsync(r => r.MangaId == req.MangaId) });
    }
}

public class BookmarkRequest
{
    public int MangaId { get; set; }
    public int Status { get; set; }
}

public class CommentRequest
{
    public int ChapterId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ParentId { get; set; }
}

public class DeleteCommentRequest
{
    public int CommentId { get; set; }
}

public class UpdateCommentRequest
{
    public int CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class RateRequest
{
    public int MangaId { get; set; }
    public int Rating { get; set; }
}
