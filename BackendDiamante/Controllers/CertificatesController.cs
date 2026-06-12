using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendDiamante.Controllers;

[Authorize(Roles = "admin,Administrador")]
public class CertificatesController : BaseController
{
    private readonly ICertificatesLogic _certificatesLogic;

    public CertificatesController(ICertificatesLogic certificatesLogic)
    {
        _certificatesLogic = certificatesLogic;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var certificates = await _certificatesLogic.GetAllAsync();
        return Success(certificates);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCertificateRequest request)
    {
        try
        {
            var certificate = await _certificatesLogic.CreateAsync(request.Name);
            return StatusCode(201, new { success = true, message = "Certificado creado correctamente.", data = certificate });
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }
}
