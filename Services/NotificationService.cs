using MangaReader.Data;
using MangaReader.Models;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, NotificationType type, string message, int? mangaId = null, int? chapterId = null, int? commentId = null, int? triggerUserId = null);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId, NotificationType? type = null);
    Task<int> GetUnreadCountAsync(int userId);
    Task DeleteNotificationAsync(int notificationId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext _context)
    {
        this._context = _context;
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateNotificationAsync(int userId, NotificationType type, string message, int? mangaId = null, int? chapterId = null, int? commentId = null, int? triggerUserId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            RelatedMangaId = mangaId,
            RelatedChapterId = chapterId,
            RelatedCommentId = commentId,
            TriggerUserId = triggerUserId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Include(n => n.TriggerUser)
            .Include(n => n.RelatedManga)
            .Include(n => n.RelatedChapter)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId, NotificationType? type = null)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead);
        
        if (type.HasValue)
        {
            query = query.Where(n => n.Type == type.Value);
        }

        var notifications = await query.ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}
