using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo invalido")]
    [MaxLength(200)]
    public string Email { get; set; } = null!;
}
