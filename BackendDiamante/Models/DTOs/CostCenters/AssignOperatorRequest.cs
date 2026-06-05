using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.CostCenters;

public class AssignOperatorRequest
{
    [Required(ErrorMessage = "El operario es requerido")]
    public int OperatorId { get; set; }
}
