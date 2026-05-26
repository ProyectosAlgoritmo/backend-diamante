using BackendDiamante.Models.DTOs.Auth;

namespace BackendDiamante.Logic.Interfaces;

public interface IAuthLogic
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<LoginResponse> RefreshTokenAsync(string token, string ipAddress);
    Task LogoutAsync(string token, string ipAddress);
    Task<UserInfoResponse?> GetCurrentUserAsync(int userId);
    Task<LoginResponse> GoogleLoginAsync(string accessToken, string ipAddress);
}
