using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Notifications;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class NotificationsLogic : INotificationsLogic
{
    private readonly ApplicationDbContext _context;

    public NotificationsLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponse> GetByUserAsync(int userId)
    {
        var items = await _context.NotificationUsers
            .AsNoTracking()
            .Where(nu => nu.UserId == userId)
            .Include(nu => nu.Notification)
                .ThenInclude(n => n.CreatedByUser)
            .OrderByDescending(nu => nu.Notification.CreatedAt)
            .ToListAsync();

        return new NotificationListResponse
        {
            Notifications = items.Select(MapToResponse).ToList(),
            TotalCount = items.Count,
            UnreadCount = items.Count(nu => !nu.IsRead),
        };
    }

    public async Task<UnreadCountResponse> GetUnreadCountAsync(int userId)
    {
        var count = await _context.NotificationUsers
            .AsNoTracking()
            .CountAsync(nu => nu.UserId == userId && !nu.IsRead);

        return new UnreadCountResponse { UnreadCount = count };
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var nu = await _context.NotificationUsers
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);

        if (nu is null) return false;
        if (nu.IsRead) return true;

        nu.IsRead = true;
        nu.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var unread = await _context.NotificationUsers
            .Where(nu => nu.UserId == userId && !nu.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var nu in unread)
        {
            nu.IsRead = true;
            nu.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<NotificationResponse> CreateNotificationAsync(
        string title, string message, string type, string severity,
        List<int> userIds, int? createdByUserId = null)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            Severity = severity,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var notificationUsers = userIds.Select(uid => new NotificationUser
        {
            NotificationId = notification.Id,
            UserId = uid,
            IsRead = false,
        }).ToList();

        _context.NotificationUsers.AddRange(notificationUsers);
        await _context.SaveChangesAsync();

        string? createdByName = null;
        if (createdByUserId.HasValue)
        {
            createdByName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == createdByUserId.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
        }

        return new NotificationResponse
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Severity = notification.Severity,
            IsRead = false,
            ReadAt = null,
            CreatedAt = notification.CreatedAt,
            CreatedByUserName = createdByName,
        };
    }

    public async Task<NotificationResponse> NotifyUserAsync(
        string title, string message, string type, string severity,
        int userId, int? createdByUserId = null)
    {
        return await CreateNotificationAsync(title, message, type, severity, [userId], createdByUserId);
    }

    public async Task<List<NotificationResponse>> NotifyUsersAsync(
        string title, string message, string type, string severity,
        List<int> userIds, int? createdByUserId = null)
    {
        var notification = await CreateNotificationAsync(title, message, type, severity, userIds, createdByUserId);
        return userIds.Select(_ => notification).ToList();
    }

    private static NotificationResponse MapToResponse(NotificationUser nu)
    {
        return new NotificationResponse
        {
            Id = nu.Notification.Id,
            Title = nu.Notification.Title,
            Message = nu.Notification.Message,
            Type = nu.Notification.Type,
            Severity = nu.Notification.Severity,
            IsRead = nu.IsRead,
            ReadAt = nu.ReadAt,
            CreatedAt = nu.Notification.CreatedAt,
            CreatedByUserName = nu.Notification.CreatedByUser?.Name,
        };
    }
}
