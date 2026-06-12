using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Certificates;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class CertificatesLogic : ICertificatesLogic
{
    private readonly ApplicationDbContext _context;

    public CertificatesLogic(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CertificateResponse>> GetAllAsync()
    {
        return await _context.Certificates
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CertificateResponse(c.Id, c.Name))
            .ToListAsync();
    }

    public async Task<CertificateResponse> CreateAsync(string name)
    {
        var trimmedName = name.Trim();

        var exists = await _context.Certificates
            .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());

        if (exists)
            throw new InvalidOperationException($"Ya existe un certificado con el nombre '{trimmedName}'.");

        var certificate = new Certificate
        {
            Name      = trimmedName,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        return new CertificateResponse(certificate.Id, certificate.Name);
    }
}
