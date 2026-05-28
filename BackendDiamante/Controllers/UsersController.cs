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

    /// <summary>Listar todos los usuarios</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _usersLogic.GetAllAsync();
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
            var deleted = await _usersLogic.DeleteAsync(id);
            if (!deleted) return Error("Usuario no encontrado", 404);
            return Success(new { }, "Usuario eliminado correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }
}
