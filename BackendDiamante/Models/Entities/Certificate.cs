namespace BackendDiamante.Models.Entities;

public class Certificate
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserCertificate> UserCertificates { get; set; } = new List<UserCertificate>();
}
