namespace BackendDiamante.Models.DTOs.Modules;

public class SubmoduleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public List<PermissionItemResponse> Permissions { get; set; } = [];
}
