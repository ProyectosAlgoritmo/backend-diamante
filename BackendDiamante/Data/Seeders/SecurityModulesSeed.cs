using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Data.Seeders;

public static class SecurityModulesSeed
{
    private sealed record PermissionDefinition(string Name, string Code);
    private sealed record SubmoduleDefinition(string Name, string Code, IReadOnlyList<PermissionDefinition> Permissions);
    private sealed record ModuleDefinition(string Name, string Code, IReadOnlyList<SubmoduleDefinition> Submodules);

    private static readonly IReadOnlyList<ModuleDefinition> CanonicalModules =
    [
        new(
            "Seguridad",
            "SECURITY",
            [
                new(
                    "Roles",
                    "SECURITY.ROLES",
                    [
                        new("Ver", "SECURITY.ROLES.VIEW"),
                        new("Crear", "SECURITY.ROLES.CREATE"),
                        new("Editar", "SECURITY.ROLES.EDIT"),
                        new("Eliminar", "SECURITY.ROLES.DELETE"),
                    ]),
                new(
                    "Usuarios",
                    "SECURITY.USERS",
                    [
                        new("Ver", "SECURITY.USERS.VIEW"),
                        new("Crear", "SECURITY.USERS.CREATE"),
                        new("Editar", "SECURITY.USERS.EDIT"),
                        new("Eliminar", "SECURITY.USERS.DELETE"),
                    ]),
                new(
                    "Configuracion",
                    "SECURITY.SETTINGS",
                    [
                        new("Ver", "SECURITY.SETTINGS.VIEW"),
                    ]),
            ]),
        new(
            "Negocio",
            "BUSINESS",
            [
                new(
                    "Empresas",
                    "BUSINESS.COMPANIES",
                    [
                        new("Ver", "BUSINESS.COMPANIES.VIEW"),
                        new("Importar", "BUSINESS.COMPANIES.IMPORT"),
                        new("Asignar personal", "BUSINESS.COMPANIES.ASSIGN"),
                        new("Editar", "BUSINESS.COMPANIES.EDIT"),
                        new("Eliminar", "BUSINESS.COMPANIES.DELETE"),
                    ]),
                new(
                    "Centros de costo",
                    "BUSINESS.COST_CENTERS",
                    [
                        new("Ver", "BUSINESS.COST_CENTERS.VIEW"),
                        new("Crear", "BUSINESS.COST_CENTERS.CREATE"),
                        new("Editar", "BUSINESS.COST_CENTERS.EDIT"),
                        new("Eliminar", "BUSINESS.COST_CENTERS.DELETE"),
                    ]),
                new(
                    "Edificios",
                    "BUSINESS.BUILDINGS",
                    [
                        new("Ver", "BUSINESS.BUILDINGS.VIEW"),
                        new("Crear", "BUSINESS.BUILDINGS.CREATE"),
                        new("Editar", "BUSINESS.BUILDINGS.EDIT"),
                        new("Eliminar", "BUSINESS.BUILDINGS.DELETE"),
                    ]),
                new(
                    "Pisos",
                    "BUSINESS.FLOORS",
                    [
                        new("Ver", "BUSINESS.FLOORS.VIEW"),
                        new("Crear", "BUSINESS.FLOORS.CREATE"),
                        new("Editar", "BUSINESS.FLOORS.EDIT"),
                        new("Eliminar", "BUSINESS.FLOORS.DELETE"),
                    ]),
                new(
                    "Asignacion de personal",
                    "BUSINESS.STAFF_ASSIGNMENT",
                    [
                        new("Ver", "BUSINESS.STAFF_ASSIGNMENT.VIEW"),
                        new("Crear", "BUSINESS.STAFF_ASSIGNMENT.CREATE"),
                        new("Editar", "BUSINESS.STAFF_ASSIGNMENT.EDIT"),
                        new("Eliminar", "BUSINESS.STAFF_ASSIGNMENT.DELETE"),
                    ]),
            ]),
    ];

    private static readonly IReadOnlyDictionary<string, string[]> LegacyPermissionMappings =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["OPERATIONAL_CONTROL.COMPANIES.VIEW"] = ["BUSINESS.COMPANIES.VIEW"],
            ["OPERATIONAL_CONTROL.COMPANIES.IMPORT"] = ["BUSINESS.COMPANIES.IMPORT"],
            ["OPERATIONAL_CONTROL.COMPANIES.ASSIGN"] = ["BUSINESS.COMPANIES.ASSIGN"],
            ["OPERATIONAL_CONTROL.COMPANIES.EDIT"] = ["BUSINESS.COMPANIES.EDIT"],
            ["OPERATIONAL_CONTROL.COMPANIES.DELETE"] = ["BUSINESS.COMPANIES.DELETE"],

            ["OPERATIONAL_CONTROL.COST_CENTERS.VIEW"] = ["BUSINESS.COST_CENTERS.VIEW", "BUSINESS.BUILDINGS.VIEW", "BUSINESS.FLOORS.VIEW"],
            ["OPERATIONAL_CONTROL.COST_CENTERS.CREATE"] = ["BUSINESS.COST_CENTERS.CREATE", "BUSINESS.BUILDINGS.CREATE", "BUSINESS.FLOORS.CREATE"],
            ["OPERATIONAL_CONTROL.COST_CENTERS.EDIT"] = ["BUSINESS.COST_CENTERS.EDIT", "BUSINESS.BUILDINGS.EDIT", "BUSINESS.FLOORS.EDIT"],
            ["OPERATIONAL_CONTROL.COST_CENTERS.DELETE"] = ["BUSINESS.COST_CENTERS.DELETE", "BUSINESS.BUILDINGS.DELETE", "BUSINESS.FLOORS.DELETE"],

            ["OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW"] = ["BUSINESS.STAFF_ASSIGNMENT.VIEW"],
            ["OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.CREATE"] = ["BUSINESS.STAFF_ASSIGNMENT.CREATE"],
            ["OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.EDIT"] = ["BUSINESS.STAFF_ASSIGNMENT.EDIT"],
            ["OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.DELETE"] = ["BUSINESS.STAFF_ASSIGNMENT.DELETE"],

            ["SECURITY.CLIENTS.VIEW"] = ["BUSINESS.COMPANIES.VIEW"],
            ["SECURITY.CLIENTS.EDIT"] = ["BUSINESS.COMPANIES.EDIT"],
            ["SECURITY.CLIENTS.DELETE"] = ["BUSINESS.COMPANIES.DELETE"],
            ["SECURITY.CLIENTS.CREATE"] = ["BUSINESS.COMPANIES.IMPORT"],
        };

    private static readonly string[] ObsoleteModuleCodes =
    [
        "OPERATIONAL_CONTROL",
    ];

    private static readonly string[] ObsoleteSubmoduleCodes =
    [
        "BUSINESS.SIZING",
        "SECURITY.EVENT_VIEWER",
        "OPERATIONAL_CONTROL.COMPANIES",
        "OPERATIONAL_CONTROL.COST_CENTERS",
        "OPERATIONAL_CONTROL.STAFF_ASSIGNMENT",
    ];

    private static readonly string[] ObsoletePermissionPrefixes =
    [
        "BUSINESS.SIZING.",
        "OPERATIONAL_CONTROL.",
        "SECURITY.CLIENTS.",
        "SECURITY.EVENT_VIEWER.",
    ];

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var modulesByCode = await EnsureCanonicalStructureAsync(context, logger, cancellationToken);
        var permissionsByCode = await context.Permissions
            .ToDictionaryAsync(permission => permission.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        await MigrateLegacyRolePermissionsAsync(context, permissionsByCode, logger, cancellationToken);
        await RemoveObsoleteCatalogAsync(context, logger, cancellationToken);
        await ReactivateCanonicalModulesAsync(context, modulesByCode, logger, cancellationToken);
    }

    private static async Task<Dictionary<string, Module>> EnsureCanonicalStructureAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var modules = await context.Modules
            .Include(module => module.Submodules)
                .ThenInclude(submodule => submodule.Permissions)
            .ToListAsync(cancellationToken);

        var modulesByCode = modules.ToDictionary(module => module.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var moduleDefinition in CanonicalModules)
        {
            if (!modulesByCode.TryGetValue(moduleDefinition.Code, out var module))
            {
                module = new Module
                {
                    Name = moduleDefinition.Name,
                    Code = moduleDefinition.Code,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                context.Modules.Add(module);
                await context.SaveChangesAsync(cancellationToken);

                modulesByCode[module.Code] = module;
                logger.LogInformation("Modulo de seguridad creado: {ModuleCode}", module.Code);
            }
            else if (!module.IsActive || !string.Equals(module.Name, moduleDefinition.Name, StringComparison.Ordinal))
            {
                module.IsActive = true;
                module.Name = moduleDefinition.Name;
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Modulo de seguridad normalizado: {ModuleCode}", module.Code);
            }

            foreach (var submoduleDefinition in moduleDefinition.Submodules)
            {
                var submodule = await context.Submodules
                    .Include(item => item.Permissions)
                    .FirstOrDefaultAsync(item => item.Code == submoduleDefinition.Code, cancellationToken);

                if (submodule is null)
                {
                    submodule = new Submodule
                    {
                        Name = submoduleDefinition.Name,
                        Code = submoduleDefinition.Code,
                        ModuleId = module.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    context.Submodules.Add(submodule);
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation("Submodulo de seguridad creado: {SubmoduleCode}", submodule.Code);
                }
                else
                {
                    var submoduleChanged = false;

                    if (!submodule.IsActive)
                    {
                        submodule.IsActive = true;
                        submoduleChanged = true;
                    }

                    if (!string.Equals(submodule.Name, submoduleDefinition.Name, StringComparison.Ordinal))
                    {
                        submodule.Name = submoduleDefinition.Name;
                        submoduleChanged = true;
                    }

                    if (submodule.ModuleId != module.Id)
                    {
                        submodule.ModuleId = module.Id;
                        submoduleChanged = true;
                    }

                    if (submoduleChanged)
                    {
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Submodulo de seguridad normalizado: {SubmoduleCode}", submodule.Code);
                    }
                }

                foreach (var permissionDefinition in submoduleDefinition.Permissions)
                    await EnsurePermissionAsync(context, submodule.Id, permissionDefinition, logger, cancellationToken);
            }
        }

        return modulesByCode;
    }

    private static async Task EnsurePermissionAsync(
        ApplicationDbContext context,
        int submoduleId,
        PermissionDefinition permissionDefinition,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Code == permissionDefinition.Code, cancellationToken);

        if (permission is null)
        {
            context.Permissions.Add(new Permission
            {
                Name = permissionDefinition.Name,
                Code = permissionDefinition.Code,
                SubmoduleId = submoduleId,
                CreatedAt = DateTime.UtcNow,
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Permiso de seguridad creado: {PermissionCode}", permissionDefinition.Code);
            return;
        }

        if (permission.SubmoduleId != submoduleId ||
            !string.Equals(permission.Name, permissionDefinition.Name, StringComparison.Ordinal))
        {
            permission.SubmoduleId = submoduleId;
            permission.Name = permissionDefinition.Name;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Permiso de seguridad normalizado: {PermissionCode}", permission.Code);
        }
    }

    private static async Task MigrateLegacyRolePermissionsAsync(
        ApplicationDbContext context,
        Dictionary<string, Permission> permissionsByCode,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingAssignments = await context.RolePermissions
            .ToListAsync(cancellationToken);

        var existingAssignmentKeys = existingAssignments
            .Select(item => $"{item.RoleId}:{item.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var migratedAssignments = 0;

        foreach (var mapping in LegacyPermissionMappings)
        {
            if (!permissionsByCode.TryGetValue(mapping.Key, out var sourcePermission))
                continue;

            var sourceAssignments = existingAssignments
                .Where(item => item.PermissionId == sourcePermission.Id)
                .ToList();

            if (sourceAssignments.Count == 0)
                continue;

            foreach (var targetCode in mapping.Value)
            {
                if (!permissionsByCode.TryGetValue(targetCode, out var targetPermission))
                    continue;

                foreach (var assignment in sourceAssignments)
                {
                    var assignmentKey = $"{assignment.RoleId}:{targetPermission.Id}";

                    if (existingAssignmentKeys.Contains(assignmentKey))
                        continue;

                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = assignment.RoleId,
                        PermissionId = targetPermission.Id,
                        GrantedAt = DateTime.UtcNow,
                    });

                    existingAssignmentKeys.Add(assignmentKey);
                    migratedAssignments++;
                }
            }
        }

        if (migratedAssignments > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Se migraron {AssignmentCount} permisos desde codigos legacy al catalogo actual.", migratedAssignments);
        }
    }

    private static async Task RemoveObsoleteCatalogAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var obsoletePermissions = (await context.Permissions.ToListAsync(cancellationToken))
            .Where(permission => ObsoletePermissionPrefixes.Any(prefix => permission.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (obsoletePermissions.Count > 0)
        {
            var obsoletePermissionIds = obsoletePermissions
                .Select(permission => permission.Id)
                .ToList();

            var rolePermissions = await context.RolePermissions
                .Where(rolePermission => obsoletePermissionIds.Contains(rolePermission.PermissionId))
                .ToListAsync(cancellationToken);

            context.RolePermissions.RemoveRange(rolePermissions);
            context.Permissions.RemoveRange(obsoletePermissions);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Se eliminaron {PermissionCount} permisos obsoletos del catalogo.", obsoletePermissions.Count);
        }

        var obsoleteSubmodules = await context.Submodules
            .Where(submodule => ObsoleteSubmoduleCodes.Contains(submodule.Code))
            .ToListAsync(cancellationToken);

        if (obsoleteSubmodules.Count > 0)
        {
            var obsoleteSubmoduleIds = obsoleteSubmodules
                .Select(submodule => submodule.Id)
                .ToList();

            var linkedPermissions = await context.Permissions
                .Where(permission => obsoleteSubmoduleIds.Contains(permission.SubmoduleId))
                .ToListAsync(cancellationToken);

            if (linkedPermissions.Count > 0)
            {
                var linkedPermissionIds = linkedPermissions
                    .Select(permission => permission.Id)
                    .ToList();

                var linkedRolePermissions = await context.RolePermissions
                    .Where(rolePermission => linkedPermissionIds.Contains(rolePermission.PermissionId))
                    .ToListAsync(cancellationToken);

                context.RolePermissions.RemoveRange(linkedRolePermissions);
                context.Permissions.RemoveRange(linkedPermissions);
            }

            context.Submodules.RemoveRange(obsoleteSubmodules);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Se eliminaron {SubmoduleCount} submodulos obsoletos.", obsoleteSubmodules.Count);
        }

        var obsoleteModules = await context.Modules
            .Where(module => ObsoleteModuleCodes.Contains(module.Code))
            .ToListAsync(cancellationToken);

        if (obsoleteModules.Count > 0)
        {
            context.Modules.RemoveRange(obsoleteModules);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Se eliminaron {ModuleCount} modulos obsoletos.", obsoleteModules.Count);
        }
    }

    private static async Task ReactivateCanonicalModulesAsync(
        ApplicationDbContext context,
        Dictionary<string, Module> modulesByCode,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var changed = false;

        foreach (var moduleDefinition in CanonicalModules)
        {
            if (!modulesByCode.TryGetValue(moduleDefinition.Code, out var module))
                continue;

            if (!module.IsActive)
            {
                module.IsActive = true;
                changed = true;
            }

            var submodules = await context.Submodules
                .Where(item => item.ModuleId == module.Id)
                .ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var submoduleDefinition in moduleDefinition.Submodules)
            {
                if (submodules.TryGetValue(submoduleDefinition.Code, out var submodule) && !submodule.IsActive)
                {
                    submodule.IsActive = true;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Se reactivaron elementos canonicos del catalogo de seguridad.");
        }
    }
}
