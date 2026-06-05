using System.Security.Claims;
using BackendDiamante.Logic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[Authorize]
public class NotificationsController : BaseController
{
    private readonly INotificationsLogic _notificationsLogic;

    public NotificationsController(INotificationsLogic notificationsLogic)
    {
        _notificationsLogic = notificationsLogic;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Error("Usuario no autenticado", 401);

        var result = await _notificationsLogic.GetByUserAsync(userId.Value);
        return Success(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Error("Usuario no autenticado", 401);

        var result = await _notificationsLogic.GetUnreadCountAsync(userId.Value);
        return Success(result);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Error("Usuario no autenticado", 401);

        var marked = await _notificationsLogic.MarkAsReadAsync(id, userId.Value);
        if (!marked) return Error("Notificación no encontrada", 404);

        return Success(new { }, "Notificación marcada como leída");
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Error("Usuario no autenticado", 401);

        var count = await _notificationsLogic.MarkAllAsReadAsync(userId.Value);
        return Success(new { markedCount = count }, "Notificaciones marcadas como leídas");
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
