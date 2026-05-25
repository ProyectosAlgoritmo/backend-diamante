using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Roles;

public class CreateRoleRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Description { get; set; }
}
