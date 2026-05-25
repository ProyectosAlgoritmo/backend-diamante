using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Roles;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class RolesLogic : IRolesLogic
{
    private readonly ApplicationDbContext _context;

    public RolesLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleResponse>> GetAllAsync()
    {
        return await _context.Roles
            .Select(r => MapToResponse(r))
            .ToListAsync();
    }

    public async Task<RoleResponse?> GetByIdAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        return role is null ? null : MapToResponse(role);
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return MapToResponse(role);
    }

    public async Task<RoleResponse?> UpdateAsync(int id, UpdateRoleRequest request)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null) return null;

        if (request.Name is not null) role.Name = request.Name;
        if (request.Description is not null) role.Description = request.Description;
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;
        role.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(role);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        return true;
    }

    private static RoleResponse MapToResponse(Role role)
    {
        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
