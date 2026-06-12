using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.CostCenters;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class CostCentersLogic : ICostCentersLogic
{
    private readonly ApplicationDbContext _context;

    public CostCentersLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    // ─── Cost Centers ─────────────────────────────────────────────────────────

    public async Task<List<CostCenterResponse>> GetAllAsync()
    {
        var costCenters = await _context.CostCenters
            .Include(cc => cc.Company)
            .Include(cc => cc.CostCenterOperators)
                .ThenInclude(cco => cco.Operator)
                    .ThenInclude(op => op.Sector)
            .Where(cc => cc.DeletedAt == null)
            .OrderBy(cc => cc.Id)
            .AsSplitQuery()
            .ToListAsync();

        return costCenters.Select(MapToResponse).ToList();
    }

    public async Task<CostCenterResponse?> GetByIdAsync(int id)
    {
        var cc = await _context.CostCenters
            .Include(c => c.Company)
            .Include(c => c.CostCenterOperators)
                .ThenInclude(cco => cco.Operator)
                    .ThenInclude(op => op.Sector)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

        return cc is null ? null : MapToResponse(cc);
    }

    public async Task<CostCenterResponse> CreateAsync(CreateCostCenterRequest request)
    {
        var company = await FindOrCreateCompanyAsync(request.CompanyName);

        var costCenter = new CostCenter
        {
            Code      = "TEMP",   // se reemplaza tras obtener el Id
            Name      = request.Name,
            Address   = request.Address,
            Areas     = request.Areas,
            CompanyId = company.Id,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.CostCenters.Add(costCenter);
        await _context.SaveChangesAsync();

        // Auto-código basado en el Id asignado
        costCenter.Code = $"CC-{costCenter.Id:D4}";
        await _context.SaveChangesAsync();

        return await GetByIdAsync(costCenter.Id) ?? MapToResponse(costCenter);
    }

    public async Task<CostCenterResponse?> UpdateAsync(int id, UpdateCostCenterRequest request)
    {
        var costCenter = await _context.CostCenters
            .FirstOrDefaultAsync(cc => cc.Id == id && cc.DeletedAt == null);

        if (costCenter is null) return null;

        var company = await FindOrCreateCompanyAsync(request.CompanyName);

        costCenter.Name      = request.Name;
        costCenter.Address   = request.Address;
        costCenter.Areas     = request.Areas;
        costCenter.CompanyId = company.Id;
        costCenter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var costCenter = await _context.CostCenters
            .FirstOrDefaultAsync(cc => cc.Id == id && cc.DeletedAt == null);

        if (costCenter is null) return false;

        // Remove all operator assignments
        var assignments = await _context.CostCenterOperators
            .Where(cco => cco.CostCenterId == id)
            .ToListAsync();

        _context.CostCenterOperators.RemoveRange(assignments);

        costCenter.DeletedAt = DateTime.UtcNow;
        costCenter.IsActive  = false;
        costCenter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var costCenter = await _context.CostCenters
            .FirstOrDefaultAsync(cc => cc.Id == id && cc.DeletedAt == null);

        if (costCenter is null) return false;

        costCenter.IsActive  = !costCenter.IsActive;
        costCenter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<CostCenterStatsResponse> GetStatsAsync()
    {
        var total          = await _context.CostCenters.CountAsync(cc => cc.DeletedAt == null);
        var active         = await _context.CostCenters.CountAsync(cc => cc.DeletedAt == null && cc.IsActive);
        var totalCompanies = await _context.Companies.CountAsync(c => !c.DeletedAt.HasValue && c.IsActive);
        var totalOperators = await _context.CostCenterOperators.CountAsync();

        return new CostCenterStatsResponse
        {
            Total          = total,
            ActiveCount    = active,
            InactiveCount  = total - active,
            TotalCompanies = totalCompanies,
            TotalOperators = totalOperators,
        };
    }

    // ─── Operators ────────────────────────────────────────────────────────────

    public async Task<List<OperatorResponse>> GetAllOperatorsAsync()
    {
        var operators = await _context.Operators
            .Include(op => op.Sector)
            .Where(op => op.IsActive)
            .OrderBy(op => op.Name)
            .ToListAsync();

        return operators.Select(MapOperatorToResponse).ToList();
    }

    public async Task<OperatorResponse?> GetOperatorByIdAsync(int id)
    {
        var op = await _context.Operators
            .Include(o => o.Sector)
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

        return op is null ? null : MapOperatorToResponse(op);
    }

    public async Task<OperatorResponse> CreateOperatorAsync(CreateOperatorRequest request)
    {
        if (request.SectorId.HasValue)
        {
            var sectorExists = await _context.Sectors.AnyAsync(s => s.Id == request.SectorId.Value);
            if (!sectorExists)
                throw new InvalidOperationException("El sector especificado no existe");
        }

        var op = new Operator
        {
            Name      = request.Name,
            Role      = request.Role,
            Shift     = request.Shift,
            SectorId  = request.SectorId,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Operators.Add(op);
        await _context.SaveChangesAsync();

        return await GetOperatorByIdAsync(op.Id) ?? MapOperatorToResponse(op);
    }

    public async Task<OperatorResponse?> UpdateOperatorAsync(int id, UpdateOperatorRequest request)
    {
        var op = await _context.Operators.FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

        if (op is null) return null;

        if (request.SectorId.HasValue)
        {
            var sectorExists = await _context.Sectors.AnyAsync(s => s.Id == request.SectorId.Value);
            if (!sectorExists)
                throw new InvalidOperationException("El sector especificado no existe");
        }

        op.Name      = request.Name;
        op.Role      = request.Role;
        op.Shift     = request.Shift;
        op.SectorId  = request.SectorId;
        op.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetOperatorByIdAsync(id);
    }

    public async Task<bool> DeleteOperatorAsync(int id)
    {
        var op = await _context.Operators.FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

        if (op is null) return false;

        // Remove all assignments for this operator
        var assignments = await _context.CostCenterOperators
            .Where(cco => cco.OperatorId == id)
            .ToListAsync();

        _context.CostCenterOperators.RemoveRange(assignments);

        op.IsActive  = false;
        op.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    // ─── Assignments ──────────────────────────────────────────────────────────

    public async Task<bool> AssignOperatorAsync(int costCenterId, int operatorId)
    {
        var costCenter = await _context.CostCenters
            .AnyAsync(cc => cc.Id == costCenterId && cc.DeletedAt == null && cc.IsActive);

        if (!costCenter)
            throw new InvalidOperationException("Centro de costo no encontrado o inactivo");

        var operatorExists = await _context.Operators
            .AnyAsync(op => op.Id == operatorId && op.IsActive);

        if (!operatorExists)
            throw new InvalidOperationException("Operario no encontrado o inactivo");

        var alreadyAssigned = await _context.CostCenterOperators
            .AnyAsync(cco => cco.CostCenterId == costCenterId && cco.OperatorId == operatorId);

        if (alreadyAssigned)
            throw new InvalidOperationException("El operario ya está asignado a este centro de costo");

        _context.CostCenterOperators.Add(new CostCenterOperator
        {
            CostCenterId = costCenterId,
            OperatorId   = operatorId,
            AssignedAt   = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnassignOperatorAsync(int costCenterId, int operatorId)
    {
        var assignment = await _context.CostCenterOperators
            .FirstOrDefaultAsync(cco => cco.CostCenterId == costCenterId && cco.OperatorId == operatorId);

        if (assignment is null) return false;

        _context.CostCenterOperators.Remove(assignment);
        await _context.SaveChangesAsync();

        return true;
    }

    // ─── Support data ─────────────────────────────────────────────────────────

    public async Task<List<CompanyResponse>> GetAllCompaniesAsync()
    {
        var companies = await _context.Companies
            .Where(c => !c.DeletedAt.HasValue && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return companies.Select(c => new CompanyResponse
        {
            Id        = c.Id,
            Name      = c.Name,
            IsActive  = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
        }).ToList();
    }

    public async Task<List<SectorResponse>> GetAllSectorsAsync()
    {
        var sectors = await _context.Sectors
            .OrderBy(s => s.Name)
            .ToListAsync();

        return sectors.Select(s => new SectorResponse
        {
            Id        = s.Id,
            Name      = s.Name,
            CreatedAt = s.CreatedAt,
        }).ToList();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Busca una empresa activa por nombre (sin importar mayúsculas/minúsculas).
    /// Si no existe, la crea automáticamente.
    /// </summary>
    private async Task<Company> FindOrCreateCompanyAsync(string companyName)
    {
        var name = companyName.Trim();

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && !c.DeletedAt.HasValue);

        if (company is not null) return company;

        company = new Company
        {
            Name      = name,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return company;
    }

    private static CostCenterResponse MapToResponse(CostCenter cc) => new()
    {
        Id          = cc.Id,
        Code        = cc.Code,
        Name        = cc.Name,
        Address     = cc.Address,
        Areas       = cc.Areas,
        CompanyId   = cc.CompanyId,
        CompanyName = cc.Company?.Name ?? string.Empty,
        IsActive    = cc.IsActive,
        CreatedAt   = cc.CreatedAt,
        UpdatedAt   = cc.UpdatedAt,
        DeletedAt   = cc.DeletedAt,
        Operators   = cc.CostCenterOperators.Select(cco => new OperatorSummaryResponse
        {
            Id         = cco.Operator.Id,
            Name       = cco.Operator.Name,
            Role       = cco.Operator.Role,
            Shift      = cco.Operator.Shift,
            SectorId   = cco.Operator.SectorId,
            SectorName = cco.Operator.Sector?.Name,
            IsActive   = cco.Operator.IsActive,
            AssignedAt = cco.AssignedAt,
        }).ToList(),
    };

    private static OperatorResponse MapOperatorToResponse(Operator op) => new()
    {
        Id         = op.Id,
        Name       = op.Name,
        Role       = op.Role,
        Shift      = op.Shift,
        SectorId   = op.SectorId,
        SectorName = op.Sector?.Name,
        IsActive   = op.IsActive,
        CreatedAt  = op.CreatedAt,
        UpdatedAt  = op.UpdatedAt,
    };
}
