using BackendDiamante.Models.DTOs.Roles;

namespace BackendDiamante.Logic.Interfaces;

public interface IRolesLogic
{
    Task<List<RoleResponse>> GetAllAsync();
    Task<RoleResponse?> GetByIdAsync(int id);
    Task<RoleResponse> CreateAsync(CreateRoleRequest request);
    Task<RoleResponse?> UpdateAsync(int id, UpdateRoleRequest request);
    Task<bool> DeleteAsync(int id);
}
