namespace BackendDiamante.Models.Entities;

public class User
{
    // ─── Campos originales (auth) — NO modificar ─────────────────────────────
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ─── Campos extendidos (modulo Usuarios) ─────────────────────────────────
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public string? DocumentId { get; set; }
    public string Status { get; set; } = "Activo";
    public bool MustChangePassword { get; set; }
    /// <summary>Columna JSON legada — se mantiene nullable para migración. Usar UserCertificates.</summary>
    public string? Certificates { get; set; }

    // ─── Relaciones ──────────────────────────────────────────────────────────
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<UserCertificate> UserCertificates { get; set; } = new List<UserCertificate>();
}
