namespace BackendDiamante.Models.DTOs.CostCenters;

public class OperatorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Shift { get; set; } = null!;
    public int? SectorId { get; set; }
    public string? SectorName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
