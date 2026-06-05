using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Success(object data, string message = "Operación exitosa") =>
        Ok(new { success = true, message, data });

    protected IActionResult Error(string message, int statusCode = 400) =>
        StatusCode(statusCode, new { success = false, message });
}
