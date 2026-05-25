using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Roles;

public class UpdateRoleRequest
{
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
