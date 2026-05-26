using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Auth;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BackendDiamante.Logic;

public class AuthLogic : IAuthLogic
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthLogic(ApplicationDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    // ─── Modelo interno para deserializar respuesta de Google userinfo ────────
    private sealed record GoogleUserInfo(
        string Sub,
        string Email,
        string? Name,
        string? Picture,
        bool Email_Verified
    );

    // ─── Login ────────────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower().Trim());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales incorrectas");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta está desactivada. Contacta al administrador.");

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return BuildLoginResponse(accessToken, refreshToken, user);
    }

    // ─── Refresh Token ────────────────────────────────────────────────────────

    public async Task<LoginResponse> RefreshTokenAsync(string token, string ipAddress)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken is null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Token de refresco inválido o expirado");

        // Rotate: revoke old, create new
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        var newRefreshToken = GenerateRefreshToken(refreshToken.UserId, ipAddress);
        _context.RefreshTokens.Add(newRefreshToken);

        var accessToken = GenerateAccessToken(refreshToken.User);
        await _context.SaveChangesAsync();

        return BuildLoginResponse(accessToken, newRefreshToken, refreshToken.User);
    }

    // ─── Logout ───────────────────────────────────────────────────────────────

    public async Task LogoutAsync(string token, string ipAddress)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken is null || !refreshToken.IsActive) return;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();
    }

    // ─── Google Login ─────────────────────────────────────────────────────────

    public async Task<LoginResponse> GoogleLoginAsync(string googleAccessToken, string ipAddress)
    {
        // Validar el access token llamando al endpoint de userinfo de Google
        GoogleUserInfo? userInfo;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", googleAccessToken);

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Token de Google inválido o expirado");

            var json = await response.Content.ReadAsStringAsync();
            userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch
        {
            throw new UnauthorizedAccessException("No se pudo verificar el token de Google");
        }

        if (userInfo is null || string.IsNullOrEmpty(userInfo.Email))
            throw new UnauthorizedAccessException("Token de Google no contiene información de usuario");

        // Buscar usuario existente por email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userInfo.Email.ToLower());

        if (user is null)
        {
            // Crear usuario nuevo con rol 'cliente' por defecto
            user = new User
            {
                Email = userInfo.Email,
                Name = userInfo.Name ?? userInfo.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = "cliente",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Necesitamos el Id antes de crear el token
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta está desactivada. Contacta al administrador.");

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return BuildLoginResponse(accessToken, refreshToken, user);
    }

    // ─── Get Current User ─────────────────────────────────────────────────────

    public async Task<UserInfoResponse?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user is null ? null : MapToUserInfo(user);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(int userId, string ipAddress)
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        var expiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays");

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedByIp = ipAddress,
            UserId = userId,
        };
    }

    private static LoginResponse BuildLoginResponse(string accessToken, RefreshToken refreshToken, User user) => new()
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken.Token,
        ExpiresAt = refreshToken.ExpiresAt,
        User = MapToUserInfo(user),
    };

    private static UserInfoResponse MapToUserInfo(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role,
    };
}
