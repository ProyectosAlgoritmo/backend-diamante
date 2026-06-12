using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "El token es requerido")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = null!;
}
