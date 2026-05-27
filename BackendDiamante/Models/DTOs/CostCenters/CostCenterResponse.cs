namespace BackendDiamante.Models.DTOs.CostCenters;

public class CostCenterResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public int Areas { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<OperatorSummaryResponse> Operators { get; set; } = [];
}
