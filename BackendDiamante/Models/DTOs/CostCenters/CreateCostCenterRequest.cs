using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.CostCenters;

public class CreateCostCenterRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres")]
    public string? Address { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Las áreas deben ser un número positivo")]
    public int Areas { get; set; }

    [Required(ErrorMessage = "El nombre de la empresa es requerido")]
    [StringLength(200, ErrorMessage = "El nombre de la empresa no puede exceder 200 caracteres")]
    public string CompanyName { get; set; } = null!;
}
