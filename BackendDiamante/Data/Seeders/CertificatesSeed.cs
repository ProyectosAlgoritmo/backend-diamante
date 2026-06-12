using System.Text.Json;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Data.Seeders;

public static class CertificatesSeed
{
    private static readonly string[] CanonicalNames =
    [
        "Certificado de alturas",
        "Certificado de quirófanos",
        "Certificado de manipulación de alimentos",
        "Certificado de bioseguridad",
        "Certificado de manejo de residuos",
    ];

    // Mapeo de nombres sin tilde (legacy) a nombres canónicos
    private static readonly Dictionary<string, string> LegacyNameMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Certificado de quirofanos"]                = "Certificado de quirófanos",
            ["Certificado de manipulacion de alimentos"] = "Certificado de manipulación de alimentos",
        };

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureCertificatesAsync(context, logger, cancellationToken);
        await MigrateUserCertificatesAsync(context, logger, cancellationToken);
    }

    private static async Task EnsureCertificatesAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingNames = await context.Certificates
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var name in CanonicalNames)
        {
            if (existingSet.Contains(name)) continue;

            context.Certificates.Add(new Certificate
            {
                Name      = name,
                IsActive  = true,
                CreatedAt = DateTime.UtcNow,
            });
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Se sembraron {Count} certificados en el catálogo.", created);
        }
    }

    private static async Task MigrateUserCertificatesAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var usersWithJson = await context.Users
            .Where(u => u.Certificates != null)
            .ToListAsync(cancellationToken);

        if (usersWithJson.Count == 0) return;

        var certMap = await context.Certificates
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var migrated = 0;

        foreach (var user in usersWithJson)
        {
            List<string> names;
            try
            {
                names = JsonSerializer.Deserialize<List<string>>(user.Certificates!) ?? [];
            }
            catch
            {
                names = [];
            }

            if (names.Count == 0)
            {
                user.Certificates = null;
                continue;
            }

            var existingCertIds = await context.UserCertificates
                .Where(uc => uc.UserId == user.Id)
                .Select(uc => uc.CertificateId)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<int>(existingCertIds);

            foreach (var rawName in names)
            {
                var canonicalName = LegacyNameMap.TryGetValue(rawName, out var mapped) ? mapped : rawName;

                if (!certMap.TryGetValue(canonicalName, out var certId)) continue;
                if (existingSet.Contains(certId)) continue;

                context.UserCertificates.Add(new UserCertificate
                {
                    UserId        = user.Id,
                    CertificateId = certId,
                    AssignedAt    = user.CreatedAt,
                });

                existingSet.Add(certId);
            }

            user.Certificates = null;
            migrated++;
        }

        if (migrated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Se migraron los certificados de {Count} usuarios al nuevo esquema relacional.", migrated);
        }
    }
}
