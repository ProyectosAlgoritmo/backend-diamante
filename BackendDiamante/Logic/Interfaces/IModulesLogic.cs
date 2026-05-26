using BackendDiamante.Models.DTOs.Modules;

namespace BackendDiamante.Logic.Interfaces;

public interface IModulesLogic
{
    Task<List<ModuleTreeResponse>> GetTreeAsync();
}
