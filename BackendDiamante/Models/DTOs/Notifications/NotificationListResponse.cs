namespace BackendDiamante.Models.DTOs.Notifications;

public class NotificationListResponse
{
    public List<NotificationResponse> Notifications { get; set; } = [];
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}
