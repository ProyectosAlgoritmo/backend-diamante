using BackendDiamante.Models.Entities;
using BackendDiamante.Security;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Data.Seeders;

public static class SecurityRolesSeed
{
    private sealed record SeedRoleDefinition(string Name, string Description);

    private static readonly SeedRoleDefinition AdministratorRoleDefinition =
        new(RoleNameResolver.AdministratorRoleName, "Rol del sistema con acceso total a todos los módulos, submódulos y permisos.");

    private static readonly SeedRoleDefinition[] DefaultRoles =
    [
        AdministratorRoleDefinition,
        new("Supervisor", "Rol base del sistema para supervisores. Se crea sin permisos asignados por defecto."),
        new("Operario", "Rol base del sistema para operarios. Se crea sin permisos asignados por defecto."),
    ];

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        Role? administratorRole = null;

        foreach (var roleDefinition in DefaultRoles)
        {
            var role = await EnsureRoleAsync(context, roleDefinition, logger, cancellationToken);

            if (string.Equals(role.Name, RoleNameResolver.AdministratorRoleName, StringComparison.OrdinalIgnoreCase))
                administratorRole = role;
        }

        if (administratorRole is null)
        {
            logger.LogWarning("No fue posible resolver el rol Administrador durante el seed de seguridad.");
            return;
        }

        await EnsureAdministratorPermissionsAsync(context, administratorRole, logger, cancellationToken);
    }

    private static async Task<Role> EnsureRoleAsync(
        ApplicationDbContext context,
        SeedRoleDefinition roleDefinition,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleDefinition.Name, cancellationToken);

        if (existingRole is not null)
        {
            var roleUpdated = false;

            if (!existingRole.IsActive)
            {
                existingRole.IsActive = true;
                roleUpdated = true;
            }

            if (existingRole.DeletedAt is not null)
            {
                existingRole.DeletedAt = null;
                roleUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(existingRole.Description) &&
                !string.IsNullOrWhiteSpace(roleDefinition.Description))
            {
                existingRole.Description = roleDefinition.Description;
                roleUpdated = true;
            }

            if (roleUpdated)
            {
                existingRole.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Rol del sistema reactivado o actualizado: {RoleName}", existingRole.Name);
            }

            return existingRole;
        }

        var role = new Role
        {
            Name = roleDefinition.Name,
            Description = roleDefinition.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Rol del sistema creado: {RoleName}", role.Name);
        return role;
    }

    private static async Task EnsureAdministratorPermissionsAsync(
        ApplicationDbContext context,
        Role administratorRole,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var totalPermissions    = await context.Permissions.CountAsync(cancellationToken);
        var assignedPermissions = await context.RolePermissions.CountAsync(rp => rp.RoleId == administratorRole.Id, cancellationToken);

        if (totalPermissions == 0)
        {
            logger.LogWarning("No hay permisos registrados en la base de datos para asignar al rol Administrador.");
            return;
        }

        if (totalPermissions == assignedPermissions)
        {
            logger.LogDebug("El rol Administrador ya tiene asignados todos los permisos actuales.");
            return;
        }

        var allPermissionIds = await context.Permissions
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);

        var assignedPermissionIds = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == administratorRole.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync(cancellationToken);

        var missingPermissionIds = allPermissionIds
            .Except(assignedPermissionIds)
            .ToList();

        if (missingPermissionIds.Count == 0)
        {
            logger.LogDebug("El rol Administrador ya tiene asignados todos los permisos actuales.");
            return;
        }

        var grantedAt = DateTime.UtcNow;

        foreach (var permissionId in missingPermissionIds)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = administratorRole.Id,
                PermissionId = permissionId,
                GrantedAt = grantedAt,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se asignaron {PermissionCount} permisos faltantes al rol Administrador.",
            missingPermissionIds.Count);
    }
}
