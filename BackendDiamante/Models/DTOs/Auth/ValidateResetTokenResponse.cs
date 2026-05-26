namespace BackendDiamante.Models.DTOs.Auth;

public class ValidateResetTokenResponse
{
    public bool IsValid { get; set; }
    public string UserName { get; set; } = null!;
    public string? Message { get; set; }
}
