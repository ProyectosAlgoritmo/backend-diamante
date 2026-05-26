using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}
