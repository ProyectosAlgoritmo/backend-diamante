using BackendDiamante.Logic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendDiamante.Security;

namespace BackendDiamante.Controllers;

[Authorize]
public class ModulesController : BaseController
{
    private readonly IModulesLogic _modulesLogic;

    public ModulesController(IModulesLogic modulesLogic)
    {
        _modulesLogic = modulesLogic;
    }

    /// <summary>Returns the full module → submodule → permission tree.</summary>
    [HttpGet]
    [RequirePermission("SECURITY.ROLES.VIEW")]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _modulesLogic.GetTreeAsync();
        return Success(tree);
    }
}
