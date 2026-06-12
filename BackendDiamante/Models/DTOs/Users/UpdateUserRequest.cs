using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Users;

public class UpdateUserRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "El correo no es válido")]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? Username { get; set; }

    [StringLength(50)]
    public string? Role { get; set; }

    public string? Status { get; set; }

    public List<string>? Certificates { get; set; }
}
