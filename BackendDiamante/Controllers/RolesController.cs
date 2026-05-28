using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendDiamante.Security;

namespace BackendDiamante.Controllers;

[Authorize]
public class RolesController : BaseController
{
    private readonly IRolesLogic _rolesLogic;

    public RolesController(IRolesLogic rolesLogic)
    {
        _rolesLogic = rolesLogic;
    }

    [HttpGet]
    [RequirePermission("SECURITY.ROLES.VIEW")]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _rolesLogic.GetAllAsync();
        return Success(roles);
    }

    [HttpGet("stats")]
    [RequirePermission("SECURITY.ROLES.VIEW")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _rolesLogic.GetStatsAsync();
        return Success(stats);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("SECURITY.ROLES.VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _rolesLogic.GetByIdAsync(id);
        if (role is null) return Error("Rol no encontrado", 404);
        return Success(role);
    }

    [HttpPost]
    [RequirePermission("SECURITY.ROLES.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var role = await _rolesLogic.CreateAsync(request);
        return Success(role, "Rol creado correctamente");
    }

    [HttpPut("{id:int}")]
    [RequirePermission("SECURITY.ROLES.EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _rolesLogic.UpdateAsync(id, request);
        if (role is null) return Error("Rol no encontrado", 404);
        return Success(role, "Rol actualizado correctamente");
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("SECURITY.ROLES.DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _rolesLogic.DeleteAsync(id);
        if (!deleted) return Error("Rol no encontrado", 404);
        return Success(new { }, "Rol eliminado correctamente");
    }
}
