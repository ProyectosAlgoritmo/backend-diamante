using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Roles;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

public class RolesController : BaseController
{
    private readonly IRolesLogic _rolesLogic;

    public RolesController(IRolesLogic rolesLogic)
    {
        _rolesLogic = rolesLogic;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _rolesLogic.GetAllAsync();
        return Success(roles);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _rolesLogic.GetStatsAsync();
        return Success(stats);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _rolesLogic.GetByIdAsync(id);
        if (role is null) return Error("Rol no encontrado", 404);
        return Success(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var role = await _rolesLogic.CreateAsync(request);
        return Success(role, "Rol creado correctamente");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _rolesLogic.UpdateAsync(id, request);
        if (role is null) return Error("Rol no encontrado", 404);
        return Success(role, "Rol actualizado correctamente");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _rolesLogic.DeleteAsync(id);
        if (!deleted) return Error("Rol no encontrado", 404);
        return Success(new { }, "Rol eliminado correctamente");
    }
}
