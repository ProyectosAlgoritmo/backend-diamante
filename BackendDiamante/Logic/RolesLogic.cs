using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Roles;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class RolesLogic : IRolesLogic
{
    private readonly ApplicationDbContext _context;

    /// <summary>Roles que no pueden editarse, eliminarse ni copiarse.</summary>
    private static readonly HashSet<string> ProtectedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Operario",
    };

    public RolesLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleResponse>> GetAllAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.Id)
            .ToListAsync();

        return roles.Select(MapToResponse).ToList();
    }

    public async Task<RoleResponse?> GetByIdAsync(int id)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        return role is null ? null : MapToResponse(role);
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            Name        = request.Name,
            Description = request.Description,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        if (request.PermissionIds.Count > 0)
            await SyncPermissionsAsync(role.Id, request.PermissionIds);

        return await GetByIdAsync(role.Id) ?? MapToResponse(role);
    }

    public async Task<RoleResponse?> UpdateAsync(int id, UpdateRoleRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (role is null) return null;

        if (ProtectedRoles.Contains(role.Name))
            throw new InvalidOperationException($"El rol '{role.Name}' no puede modificarse.");

        if (request.Name        is not null) role.Name        = request.Name;
        if (request.Description is not null) role.Description = request.Description;
        if (request.IsActive.HasValue)       role.IsActive     = request.IsActive.Value;
        role.UpdatedAt = DateTime.UtcNow;

        if (request.PermissionIds is not null)
            await SyncPermissionsAsync(role.Id, request.PermissionIds);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(role.Id);
    }

    /// <summary>
    /// Soft delete — sets DeletedAt, marks IsActive = false,
    /// removes all role permissions, and does NOT remove the row.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (role is null) return false;

        if (ProtectedRoles.Contains(role.Name))
            throw new InvalidOperationException($"El rol '{role.Name}' no puede eliminarse.");

        // Remove all permissions assigned to this role
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(rolePermissions);

        role.DeletedAt = DateTime.UtcNow;
        role.IsActive  = false;
        role.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<RoleStatsResponse> GetStatsAsync()
    {
        var total  = await _context.Roles.CountAsync(r => r.DeletedAt == null);
        var active = await _context.Roles.CountAsync(r => r.DeletedAt == null && r.IsActive);

        return new RoleStatsResponse
        {
            Total         = total,
            ActiveCount   = active,
            InactiveCount = total - active,
        };
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task SyncPermissionsAsync(int roleId, List<int> permissionIds)
    {
        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existing);

        var validIds = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var permId in validIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId       = roleId,
                PermissionId = permId,
                GrantedAt    = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
    }

    private static RoleResponse MapToResponse(Role role) => new()
    {
        Id            = role.Id,
        Name          = role.Name,
        Description   = role.Description,
        IsActive      = role.IsActive,
        CreatedAt     = role.CreatedAt,
        UpdatedAt     = role.UpdatedAt,
        DeletedAt     = role.DeletedAt,
        PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList(),
    };
}
