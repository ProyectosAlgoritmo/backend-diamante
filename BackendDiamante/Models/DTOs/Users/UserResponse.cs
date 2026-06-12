using BackendDiamante.Models.DTOs.Certificates;

namespace BackendDiamante.Models.DTOs.Users;

public class UserResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? DocumentId { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<CertificateResponse> Certificates { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
