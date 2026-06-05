using BackendDiamante.Models.DTOs.Users;

namespace BackendDiamante.Logic.Interfaces;

public interface IUsersLogic
{
    Task<List<UserResponse>> GetAllAsync();
    Task<UserResponse?> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteAsync(int id);
    Task<UserStatsResponse> GetStatsAsync();
}
