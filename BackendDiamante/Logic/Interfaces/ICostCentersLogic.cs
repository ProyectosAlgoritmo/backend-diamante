using BackendDiamante.Models.DTOs.CostCenters;

namespace BackendDiamante.Logic.Interfaces;

public interface ICostCentersLogic
{
    // ─── Cost Centers ─────────────────────────────────────────────────────────
    Task<List<CostCenterResponse>> GetAllAsync();
    Task<CostCenterResponse?> GetByIdAsync(int id);
    Task<CostCenterResponse> CreateAsync(CreateCostCenterRequest request);
    Task<CostCenterResponse?> UpdateAsync(int id, UpdateCostCenterRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleStatusAsync(int id);
    Task<CostCenterStatsResponse> GetStatsAsync();

    // ─── Operators ────────────────────────────────────────────────────────────
    Task<List<OperatorResponse>> GetAllOperatorsAsync();
    Task<OperatorResponse?> GetOperatorByIdAsync(int id);
    Task<OperatorResponse> CreateOperatorAsync(CreateOperatorRequest request);
    Task<OperatorResponse?> UpdateOperatorAsync(int id, UpdateOperatorRequest request);
    Task<bool> DeleteOperatorAsync(int id);

    // ─── Assignments ──────────────────────────────────────────────────────────
    Task<bool> AssignOperatorAsync(int costCenterId, int operatorId);
    Task<bool> UnassignOperatorAsync(int costCenterId, int operatorId);

    // ─── Support data ─────────────────────────────────────────────────────────
    Task<List<CompanyResponse>> GetAllCompaniesAsync();
    Task<List<SectorResponse>> GetAllSectorsAsync();
}
