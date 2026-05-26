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
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ip = GetIpAddress();
            var result = await _authLogic.LoginAsync(request, ip);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ip = GetIpAddress();
            var result = await _authLogic.RefreshTokenAsync(request.RefreshToken, ip);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
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
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var ip = GetIpAddress();
            var result = await _authLogic.GoogleLoginAsync(request.AccessToken, ip);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST api/auth/social/{provider}  — stub para proveedores no configurados
    // La constraint regex excluye "google" para que nunca capture esa ruta específica
    [HttpPost("social/{provider:regex(^(?!google$)[[a-z]]+$)}")]
    public IActionResult SocialLogin(string provider)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = $"Login con {provider} no está disponible aún. Próximamente."
        });
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
}
