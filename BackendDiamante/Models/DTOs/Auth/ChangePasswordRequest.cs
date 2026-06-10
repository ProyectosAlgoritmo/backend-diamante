using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "La contrasena actual es requerida")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "La nueva contrasena es requerida")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres")]
    public string NewPassword { get; set; } = null!;
}
