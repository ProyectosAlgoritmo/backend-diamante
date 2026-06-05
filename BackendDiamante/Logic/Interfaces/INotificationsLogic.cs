using BackendDiamante.Models.DTOs.Notifications;

namespace BackendDiamante.Logic.Interfaces;

public interface INotificationsLogic
{
    Task<NotificationListResponse> GetByUserAsync(int userId);
    Task<UnreadCountResponse> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<int> MarkAllAsReadAsync(int userId);

    Task<NotificationResponse> CreateNotificationAsync(string title, string message, string type, string severity, List<int> userIds, int? createdByUserId = null);
    Task<NotificationResponse> NotifyUserAsync(string title, string message, string type, string severity, int userId, int? createdByUserId = null);
    Task<List<NotificationResponse>> NotifyUsersAsync(string title, string message, string type, string severity, List<int> userIds, int? createdByUserId = null);
}
