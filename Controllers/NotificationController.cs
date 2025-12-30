using System.Security.Claims;
using MangaReader.Models;
using MangaReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

        // Return only necessary fields to avoid circular references and reduce payload size
        var safeNotifications = notifications.Select(n => new
        {
            id = n.Id,
            type = (int)n.Type,
            message = n.Message,
            isRead = n.IsRead,
            createdAt = n.CreatedAt,
            relatedMangaId = n.RelatedMangaId,
            relatedChapterId = n.RelatedChapterId,
            relatedCommentId = n.RelatedCommentId,
            triggerUser = n.TriggerUser != null ? new { username = n.TriggerUser.Username, avatarUrl = n.TriggerUser.AvatarUrl } : null
        }).ToList();

        return Json(new { success = true, notifications = safeNotifications, unreadCount });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead(string? tab = null)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (tab == "system")
        {
            await _notificationService.MarkAllAsReadAsync(userId, NotificationType.System);
            await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Reward);
        }
        else if (tab == "series")
        {
            await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Comic);
        }
        else if (tab == "community")
        {
            await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Community);
        }
        else
        {
            await _notificationService.MarkAllAsReadAsync(userId, null);
        }
        
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Json(new { count = 0 });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }
}
