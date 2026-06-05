namespace BackendDiamante.Models.DTOs.Modules;

public class ModuleTreeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public List<SubmoduleResponse> Submodules { get; set; } = [];
}
