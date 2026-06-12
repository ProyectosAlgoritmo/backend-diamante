using System.Security.Claims;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackendDiamante.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthLogic _authLogic;

    public AuthController(IAuthLogic authLogic)
    {
        _authLogic = authLogic;
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ip = GetIpAddress();
        var result = await _authLogic.LoginAsync(request, ip);
        return Ok(result);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ip = GetIpAddress();
        var result = await _authLogic.RefreshTokenAsync(request.RefreshToken, ip);
        return Ok(result);
    }

    // POST api/auth/logout  (requiere JWT válido)
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var ip = GetIpAddress();
        await _authLogic.LogoutAsync(request.RefreshToken, ip);
        return NoContent();
    }

    // GET api/auth/me  (requiere JWT válido)
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Token inválido" });

        var user = await _authLogic.GetCurrentUserAsync(userId);
        if (user is null) return NotFound(new { message = "Usuario no encontrado" });

        return Ok(user);
    }

    // POST api/auth/social/google
    [HttpPost("social/google")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var ip = GetIpAddress();
        var result = await _authLogic.GoogleLoginAsync(request.AccessToken, ip);
        return Ok(result);
    }

    // POST api/auth/social/microsoft
    [HttpPost("social/microsoft")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> MicrosoftLogin([FromBody] MicrosoftLoginRequest request)
    {
        var ip = GetIpAddress();
        var result = await _authLogic.MicrosoftLoginAsync(request.AccessToken, ip);
        return Ok(result);
    }

    // POST api/auth/social/{provider}  — stub para proveedores no configurados
    [HttpPost("social/{provider:regex(^(?!google$|microsoft$)[[a-z]]+$)}")]
    [AllowAnonymous]
    public IActionResult SocialLogin(string provider)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = $"Login con {provider} no está disponible aún. Próximamente."
        });
    }

    // POST api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Obtener base URL del frontend desde el header Origin o fallback local
        var frontendUrl = Request.Headers.Origin.FirstOrDefault() ?? "http://localhost:5173";

        await _authLogic.ForgotPasswordAsync(request.Email, frontendUrl);

        // Siempre responder igual — anti-enumeración de usuarios
        return Ok(new
        {
            message = "Si el correo existe en nuestro sistema, recibirás un enlace de recuperación."
        });
    }

    // GET api/auth/validate-reset-token?token=xxx
    [HttpGet("validate-reset-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidateResetTokenResponse>> ValidateResetToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token requerido" });

        var result = await _authLogic.ValidateResetTokenAsync(token);
        return Ok(result);
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authLogic.ResetPasswordAsync(request.Token, request.NewPassword);
        return Ok(new { message = "Contraseña actualizada exitosamente." });
    }

    // POST api/auth/change-password (requiere JWT válido)
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Token inválido" });

        await _authLogic.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return Ok(new { message = "Contraseña actualizada exitosamente." });
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
}
