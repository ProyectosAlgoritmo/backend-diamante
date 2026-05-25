using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Roles;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRolesLogic _rolesLogic;

    public RolesController(IRolesLogic rolesLogic)
    {
        _rolesLogic = rolesLogic;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleResponse>>> GetAll()
    {
        var roles = await _rolesLogic.GetAllAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleResponse>> GetById(int id)
    {
        var role = await _rolesLogic.GetByIdAsync(id);
        if (role is null) return NotFound();
        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request)
    {
        var role = await _rolesLogic.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleResponse>> Update(int id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _rolesLogic.UpdateAsync(id, request);
        if (role is null) return NotFound();
        return Ok(role);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _rolesLogic.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
