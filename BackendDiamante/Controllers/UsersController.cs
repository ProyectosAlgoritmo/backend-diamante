using System.Security.Claims;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly IUsersLogic _usersLogic;

    public UsersController(IUsersLogic usersLogic)
    {
        _usersLogic = usersLogic;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    /// <summary>Listar todos los usuarios</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _usersLogic.GetAllAsync(GetCurrentUserId());
        return Success(users);
    }

    /// <summary>Estadisticas de usuarios (activos, inactivos, total)</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _usersLogic.GetStatsAsync();
        return Success(stats);
    }

    /// <summary>Obtener un usuario por ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _usersLogic.GetByIdAsync(id);
        if (user is null) return Error("Usuario no encontrado", 404);
        return Success(user);
    }

    /// <summary>Roles activos asignables al crear o editar un usuario</summary>
    [HttpGet("assignable-roles")]
    [Authorize(Roles = "admin,Administrador")]
    public async Task<IActionResult> GetAssignableRoles()
    {
        var roles = await _usersLogic.GetAssignableRolesAsync();
        return Success(roles);
    }

    /// <summary>Crear un nuevo usuario (solo admin)</summary>
    [HttpPost]
    [Authorize(Roles = "admin,Administrador")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _usersLogic.CreateAsync(request);
            return Success(user, "Usuario creado correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }

    /// <summary>Actualizar un usuario existente (solo admin)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _usersLogic.UpdateAsync(id, request);
            if (user is null) return Error("Usuario no encontrado", 404);
            return Success(user, "Usuario actualizado correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }

    /// <summary>Eliminar un usuario (solo admin)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _usersLogic.DeleteAsync(id, GetCurrentUserId());
            if (!deleted) return Error("Usuario no encontrado", 404);
            return Success(new { }, "Usuario eliminado correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }
}
