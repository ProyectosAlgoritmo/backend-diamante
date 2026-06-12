using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class LoginRequest
{
    /// <summary>Correo electrónico o nombre de usuario.</summary>
    [Required(ErrorMessage = "El correo o usuario es requerido")]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres")]
    [MaxLength(100)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}
