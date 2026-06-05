using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Auth;

public class GoogleLoginRequest
{
    /// <summary>
    /// Access Token OAuth2 que @react-oauth/google entrega al frontend
    /// (flujo implícito de useGoogleLogin). El backend lo valida llamando
    /// al endpoint de userinfo de Google: GET /oauth2/v3/userinfo.
    /// </summary>
    [Required(ErrorMessage = "El access token de Google es requerido")]
    public string AccessToken { get; set; } = null!;
}
