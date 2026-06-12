using System.Security.Claims;
using BackendDiamante.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BackendDiamante.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(15);

    public SessionValidationMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdClaim  = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.User.FindFirstValue("sub");
        var sessionIdClaim = context.User.FindFirstValue("sid");

        // Tokens anteriores al feature (sin claim sid): dejar pasar hasta que expiren
        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrEmpty(sessionIdClaim))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"sess_{userId}";
        if (!_cache.TryGetValue(cacheKey, out string? activeSessionId))
        {
            activeSessionId = await dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => u.ActiveSessionId)
                .FirstOrDefaultAsync(context.RequestAborted);

            if (activeSessionId is not null)
                _cache.Set(cacheKey, activeSessionId, CacheTtl);
        }

        if (activeSessionId != sessionIdClaim)
        {
            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Tu sesión fue cerrada porque iniciaste sesión en otro dispositivo."
            });
            return;
        }

        await _next(context);
    }
}
