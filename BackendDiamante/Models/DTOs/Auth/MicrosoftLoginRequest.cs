using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

/// <summary>
/// Access Token OAuth2 que MSAL entrega al frontend (flujo popup).
/// El backend lo valida llamando al endpoint de Microsoft Graph:
/// GET https://graph.microsoft.com/v1.0/me
/// </summary>
public class MicrosoftLoginRequest
{
    [Required(ErrorMessage = "El access token de Microsoft es requerido")]
    public string AccessToken { get; set; } = null!;
}
