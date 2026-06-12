using System.ComponentModel.DataAnnotations;

namespace BackendDiamante.Models.DTOs.Certificates;

public class CreateCertificateRequest
{
    [Required(ErrorMessage = "El nombre del certificado es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = null!;
}
