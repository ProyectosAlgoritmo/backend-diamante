namespace BackendDiamante.Models.DTOs.Auth;

public class UserInfoResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Role { get; set; } = null!;
    public List<string> Permissions { get; set; } = [];
    public bool MustChangePassword { get; set; }
}
