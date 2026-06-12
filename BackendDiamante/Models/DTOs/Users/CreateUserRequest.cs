using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Users;

public class CreateUserRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    [StringLength(200)]
    public string Email { get; set; } = null!;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(30)]
    public string? DocumentId { get; set; }

    [StringLength(50)]
    public string? Username { get; set; }

    [Required(ErrorMessage = "El rol es requerido")]
    [StringLength(50)]
    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public List<string> Certificates { get; set; } = [];
}
