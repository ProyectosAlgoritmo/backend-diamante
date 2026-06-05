using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Modules;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class ModulesLogic : IModulesLogic
{
    private readonly ApplicationDbContext _context;

    public ModulesLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ModuleTreeResponse>> GetTreeAsync()
    {
        var modules = await _context.Modules
            .Where(m => m.IsActive)
            .Include(m => m.Submodules.Where(s => s.IsActive))
                .ThenInclude(s => s.Permissions)
            .OrderBy(m => m.Id)
            .ToListAsync();

        return modules.Select(m => new ModuleTreeResponse
        {
            Id   = m.Id,
            Name = m.Name,
            Code = m.Code,
            Submodules = m.Submodules
                .OrderBy(s => s.Id)
                .Select(s => new SubmoduleResponse
                {
                    Id   = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Permissions = s.Permissions
                        .OrderBy(p => p.Id)
                        .Select(p => new PermissionItemResponse
                        {
                            Id   = p.Id,
                            Name = p.Name,
                            Code = p.Code
                        }).ToList()
                }).ToList()
        }).ToList();
    }
}
