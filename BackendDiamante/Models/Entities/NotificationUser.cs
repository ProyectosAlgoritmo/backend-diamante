namespace BackendDiamante.Models.Entities;

public class NotificationUser
{
    public int Id { get; set; }

    public int NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
