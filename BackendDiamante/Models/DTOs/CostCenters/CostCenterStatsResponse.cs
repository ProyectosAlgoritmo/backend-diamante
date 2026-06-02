namespace BackendDiamante.Models.DTOs.CostCenters;

public class CostCenterStatsResponse
{
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalOperators { get; set; }
}
