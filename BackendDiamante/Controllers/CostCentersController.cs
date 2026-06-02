using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.CostCenters;
using BackendDiamante.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[Authorize]
public class CostCentersController : BaseController
{
    private readonly ICostCentersLogic _logic;

    public CostCentersController(ICostCentersLogic logic)
    {
        _logic = logic;
    }

    // ─── Cost Centers ─────────────────────────────────────────────────────────

    [HttpGet]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.VIEW")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _logic.GetAllAsync();
        return Success(list);
    }

    [HttpGet("stats")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.VIEW")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _logic.GetStatsAsync();
        return Success(stats);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var cc = await _logic.GetByIdAsync(id);
        if (cc is null) return Error("Centro de costo no encontrado", 404);
        return Success(cc);
    }

    [HttpPost]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateCostCenterRequest request)
    {
        var cc = await _logic.CreateAsync(request);
        return Success(cc, "Centro de costo creado correctamente");
    }

    [HttpPut("{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCostCenterRequest request)
    {
        var cc = await _logic.UpdateAsync(id, request);
        if (cc is null) return Error("Centro de costo no encontrado", 404);
        return Success(cc, "Centro de costo actualizado correctamente");
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _logic.DeleteAsync(id);
        if (!deleted) return Error("Centro de costo no encontrado", 404);
        return Success(new { }, "Centro de costo eliminado correctamente");
    }

    [HttpPatch("{id:int}/toggle-status")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.EDIT")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var toggled = await _logic.ToggleStatusAsync(id);
        if (!toggled) return Error("Centro de costo no encontrado", 404);
        return Success(new { }, "Estado actualizado correctamente");
    }

    // ─── Operators ────────────────────────────────────────────────────────────

    [HttpGet("operators")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW")]
    public async Task<IActionResult> GetAllOperators()
    {
        var operators = await _logic.GetAllOperatorsAsync();
        return Success(operators);
    }

    [HttpGet("operators/{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW")]
    public async Task<IActionResult> GetOperatorById(int id)
    {
        var op = await _logic.GetOperatorByIdAsync(id);
        if (op is null) return Error("Operario no encontrado", 404);
        return Success(op);
    }

    [HttpPost("operators")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.CREATE")]
    public async Task<IActionResult> CreateOperator([FromBody] CreateOperatorRequest request)
    {
        var op = await _logic.CreateOperatorAsync(request);
        return Success(op, "Operario creado correctamente");
    }

    [HttpPut("operators/{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.EDIT")]
    public async Task<IActionResult> UpdateOperator(int id, [FromBody] UpdateOperatorRequest request)
    {
        var op = await _logic.UpdateOperatorAsync(id, request);
        if (op is null) return Error("Operario no encontrado", 404);
        return Success(op, "Operario actualizado correctamente");
    }

    [HttpDelete("operators/{id:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.DELETE")]
    public async Task<IActionResult> DeleteOperator(int id)
    {
        var deleted = await _logic.DeleteOperatorAsync(id);
        if (!deleted) return Error("Operario no encontrado", 404);
        return Success(new { }, "Operario eliminado correctamente");
    }

    // ─── Assignments ──────────────────────────────────────────────────────────

    [HttpPost("{id:int}/operators")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.CREATE")]
    public async Task<IActionResult> AssignOperator(int id, [FromBody] AssignOperatorRequest request)
    {
        await _logic.AssignOperatorAsync(id, request.OperatorId);
        return Success(new { }, "Operario asignado correctamente");
    }

    [HttpDelete("{id:int}/operators/{operatorId:int}")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.DELETE")]
    public async Task<IActionResult> UnassignOperator(int id, int operatorId)
    {
        var removed = await _logic.UnassignOperatorAsync(id, operatorId);
        if (!removed) return Error("Asignación no encontrada", 404);
        return Success(new { }, "Operario removido del centro de costo");
    }

    // ─── Support data ─────────────────────────────────────────────────────────

    [HttpGet("companies")]
    [RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.VIEW")]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await _logic.GetAllCompaniesAsync();
        return Success(companies);
    }

    [HttpGet("sectors")]
    [RequirePermission("OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW")]
    public async Task<IActionResult> GetSectors()
    {
        var sectors = await _logic.GetAllSectorsAsync();
        return Success(sectors);
    }
}
