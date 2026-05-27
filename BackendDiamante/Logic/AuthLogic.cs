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
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthLogic> _logger;

    public AuthLogic(
        ApplicationDbContext context,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        IEmailService emailService,
        ILogger<AuthLogic> logger)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _emailService = emailService;
        _logger = logger;
    }

    // ─── Validacion de dominio empresarial ──────────────────────────────────
    private void ValidateDomain(string email)
    {
        var allowedDomain = _config["Auth:AllowedDomain"];
        if (string.IsNullOrEmpty(allowedDomain)) return; // Si no esta configurado, no restringir

        var domain = email.Trim().ToLower().Split('@').LastOrDefault();
        if (!string.Equals(domain, allowedDomain.Trim().ToLower(), StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Acceso restringido a cuentas empresariales @" + allowedDomain);
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
        var email = request.Email.Trim().ToLower();
        ValidateDomain(email);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Usuario no autorizado");

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

        ValidateDomain(userInfo.Email);

        // Buscar usuario existente — NO crear automaticamente
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userInfo.Email.Trim().ToLower());

        if (user is null)
            throw new UnauthorizedAccessException("Usuario no autorizado");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta está desactivada. Contacta al administrador.");

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return BuildLoginResponse(accessToken, refreshToken, user);
    }

    // ─── Microsoft Login ───────────────────────────────────────────────────────

    /// <summary>Modelo interno para deserializar respuesta de Microsoft Graph /me</summary>
    private sealed record MicrosoftUserInfo(
        string? Id,
        string? DisplayName,
        string? Mail,
        string? UserPrincipalName
    );

    public async Task<LoginResponse> MicrosoftLoginAsync(string microsoftAccessToken, string ipAddress)
    {
        MicrosoftUserInfo? userInfo;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", microsoftAccessToken);

            var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");

            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Token de Microsoft invalido o expirado");

            var json = await response.Content.ReadAsStringAsync();
            userInfo = JsonSerializer.Deserialize<MicrosoftUserInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch
        {
            throw new UnauthorizedAccessException("No se pudo verificar el token de Microsoft");
        }

        // Microsoft Graph: email puede estar en Mail o UserPrincipalName
        var email = userInfo?.Mail ?? userInfo?.UserPrincipalName;

        if (userInfo is null || string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("Token de Microsoft no contiene informacion de usuario");

        ValidateDomain(email);

        // Buscar usuario existente — NO crear automaticamente
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());

        if (user is null)
            throw new UnauthorizedAccessException("Usuario no autorizado");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta esta desactivada. Contacta al administrador.");

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

    // ─── Forgot Password ──────────────────────────────────────────────────────

    public async Task ForgotPasswordAsync(string email, string frontendBaseUrl)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower().Trim());

        // No revelar si el correo existe o no (anti-enumeracion)
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Solicitud de recuperacion para correo inexistente o inactivo: {Email}", email);
            return;
        }

        // Invalidar tokens previos no usados del mismo usuario
        var previousTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var old in previousTokens)
            old.IsUsed = true;

        // Generar token seguro
        var resetToken = GeneratePasswordResetToken(user.Id);
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // Construir enlace de recuperacion
        var resetLink = $"{frontendBaseUrl.TrimEnd('/')}/restablecer-contrasena?token={Uri.EscapeDataString(resetToken.Token)}";

        // Enviar correo
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar correo de recuperacion a {Email}", email);
            // No propagar la excepcion — el endpoint siempre responde igual
        }
    }

    // ─── Validate Reset Token ────────────────────────────────────────────────

    public async Task<ValidateResetTokenResponse> ValidateResetTokenAsync(string token)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken is null)
            return new ValidateResetTokenResponse
            {
                IsValid = false,
                UserName = string.Empty,
                Message = "El enlace de recuperacion no es valido."
            };

        if (resetToken.IsUsed)
            return new ValidateResetTokenResponse
            {
                IsValid = false,
                UserName = resetToken.User.Name,
                Message = "Este enlace ya fue utilizado. Solicita uno nuevo."
            };

        if (resetToken.IsExpired)
            return new ValidateResetTokenResponse
            {
                IsValid = false,
                UserName = resetToken.User.Name,
                Message = "El enlace ha expirado. Solicita uno nuevo."
            };

        return new ValidateResetTokenResponse
        {
            IsValid = true,
            UserName = resetToken.User.Name,
        };
    }

    // ─── Reset Password ──────────────────────────────────────────────────────

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken is null)
            throw new InvalidOperationException("Token de recuperacion invalido.");

        if (resetToken.IsUsed)
            throw new InvalidOperationException("Este enlace ya fue utilizado. Solicita uno nuevo.");

        if (resetToken.IsExpired)
            throw new InvalidOperationException("El enlace ha expirado. Solicita uno nuevo.");

        // Actualizar contrasena
        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;

        // Marcar token como usado
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;

        // Revocar todos los refresh tokens activos del usuario (forzar re-login)
        var activeRefreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == resetToken.UserId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var rt in activeRefreshTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = "password-reset";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Contrasena actualizada exitosamente para usuario {UserId}", resetToken.UserId);
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

    private PasswordResetToken GeneratePasswordResetToken(int userId)
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        // URL-safe Base64 (reemplazar +/ por -_ y quitar =)
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var expiryMinutes = _config.GetValue("Email:ResetTokenExpirationMinutes", 60);

        return new PasswordResetToken
        {
            Token     = tokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            UserId    = userId,
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
