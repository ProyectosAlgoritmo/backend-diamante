using System.Security.Claims;
using BackendDiamante.Data;
using BackendDiamante.Security;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Middleware;

public class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
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

        var userRoleName = await dbContext.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(context.RequestAborted);

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

        var userPermissions = await dbContext.Roles
            .Where(r => r.Name == resolvedRoleName && r.IsActive && r.DeletedAt == null)
            .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(context.RequestAborted);

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
