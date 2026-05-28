using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.CostCenters;

public class UpdateOperatorRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "El rol es requerido")]
    [StringLength(100, ErrorMessage = "El rol no puede exceder 100 caracteres")]
    public string Role { get; set; } = null!;

    [Required(ErrorMessage = "El turno es requerido")]
    [StringLength(50, ErrorMessage = "El turno no puede exceder 50 caracteres")]
    public string Shift { get; set; } = null!;

    public int? SectorId { get; set; }
}
