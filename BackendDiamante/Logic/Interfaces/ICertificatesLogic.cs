using BackendDiamante.Models.DTOs.Certificates;

namespace BackendDiamante.Logic.Interfaces;

public interface ICertificatesLogic
{
    Task<List<CertificateResponse>> GetAllAsync();
    Task<CertificateResponse> CreateAsync(string name);
}
