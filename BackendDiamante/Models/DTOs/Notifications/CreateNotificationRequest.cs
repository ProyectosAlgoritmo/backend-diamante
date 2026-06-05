namespace BackendDiamante.Models.DTOs.Notifications;

public class CreateNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public List<int> UserIds { get; set; } = [];
}
