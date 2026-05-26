using BackendDiamante.Logic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

public class ModulesController : BaseController
{
    private readonly IModulesLogic _modulesLogic;

    public ModulesController(IModulesLogic modulesLogic)
    {
        _modulesLogic = modulesLogic;
    }

    /// <summary>Returns the full module → submodule → permission tree.</summary>
    [HttpGet]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _modulesLogic.GetTreeAsync();
        return Success(tree);
    }
}
