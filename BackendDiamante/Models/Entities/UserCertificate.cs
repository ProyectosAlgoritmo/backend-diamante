namespace BackendDiamante.Models.Entities;

public class UserCertificate
{
    public int UserId { get; set; }
    public int CertificateId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Certificate Certificate { get; set; } = null!;
}
