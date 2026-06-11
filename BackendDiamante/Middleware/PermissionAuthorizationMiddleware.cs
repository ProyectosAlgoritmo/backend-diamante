using System.Security.Claims;
using BackendDiamante.Data;
using BackendDiamante.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BackendDiamante.Middleware;

public class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public PermissionAuthorizationMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var endpoint = context.GetEndpoint();
        var requiredPermissions = endpoint?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .Select(attr => attr.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requiredPermissions is null || requiredPermissions.Length == 0)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized,
                "Tu sesion no es valida o expiro.");
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdValue, out var userId))
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized,
                "Tu sesion no es valida o expiro.");
            return;
        }

        // Obtener rol del usuario desde caché o BD
        var userRoleName = await GetUserRoleAsync(userId, dbContext, context.RequestAborted);

        if (string.IsNullOrWhiteSpace(userRoleName))
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status403Forbidden,
                "No tienes permisos para realizar esta accion.");
            return;
        }

        var resolvedRoleName = RoleNameResolver.Resolve(userRoleName);

        if (RoleNameResolver.IsAdministrator(resolvedRoleName))
        {
            await _next(context);
            return;
        }

        // Obtener permisos del rol desde caché o BD
        var userPermissions = await GetRolePermissionsAsync(resolvedRoleName, dbContext, context.RequestAborted);

        var hasAllPermissions = requiredPermissions.All(required =>
            userPermissions.Contains(required, StringComparer.OrdinalIgnoreCase));

        if (!hasAllPermissions)
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status403Forbidden,
                "No tienes permisos para realizar esta accion.");
            return;
        }

        await _next(context);
    }

    private async Task<string?> GetUserRoleAsync(int userId, ApplicationDbContext dbContext, CancellationToken ct)
    {
        var cacheKey = $"user_role_{userId}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var role = await dbContext.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(ct);

        if (role is not null)
            _cache.Set(cacheKey, role, CacheTtl);

        return role;
    }

    private async Task<List<string>> GetRolePermissionsAsync(string roleName, ApplicationDbContext dbContext, CancellationToken ct)
    {
        var cacheKey = $"role_perms_{roleName}";
        if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached is not null)
            return cached;

        var permissions = await dbContext.Roles
            .Where(r => r.Name == roleName && r.IsActive && r.DeletedAt == null)
            .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(ct);

        _cache.Set(cacheKey, permissions, CacheTtl);
        return permissions;
    }

    private static async Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message
        });
    }
}
