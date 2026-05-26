using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "El token es requerido")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "La nueva contrasena es requerida")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "La confirmacion de contrasena es requerida")]
    [Compare("NewPassword", ErrorMessage = "Las contrasenas no coinciden")]
    public string ConfirmPassword { get; set; } = null!;
}
