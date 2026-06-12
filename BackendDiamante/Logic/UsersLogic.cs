using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Certificates;
using BackendDiamante.Models.DTOs.Users;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class UsersLogic : IUsersLogic
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsersLogic> _logger;

    /// <summary>Roles protegidos que no se pueden asignar, editar ni eliminar desde el módulo Usuarios.</summary>
    private static readonly HashSet<string> ProtectedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "Administrador",
    };

    public UsersLogic(ApplicationDbContext context, IEmailService emailService, ILogger<UsersLogic> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    private static bool IsProtectedUser(User user) =>
        ProtectedRoles.Contains(user.Role);

    // ── GET ALL (excluye al usuario actual) ──────────────────────────────────
    public async Task<List<UserResponse>> GetAllAsync(int currentUserId)
    {
        var users = await _context.Users
            .Include(u => u.UserCertificates).ThenInclude(uc => uc.Certificate)
            .OrderByDescending(u => u.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();

        return users
            .Where(u => u.Id != currentUserId)
            .Select(MapToResponse)
            .ToList();
    }

    // ── GET BY ID ────────────────────────────────────────────────────────────
    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserCertificates).ThenInclude(uc => uc.Certificate)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return null;
        return MapToResponse(user);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // La cédula es el identificador único entre todos los usuarios
        if (!string.IsNullOrWhiteSpace(request.DocumentId))
        {
            var existingByDoc = await _context.Users
                .FirstOrDefaultAsync(u => u.DocumentId != null
                    && u.DocumentId == request.DocumentId.Trim());

            if (existingByDoc is not null)
            {
                if (existingByDoc.Status == "Activo" || existingByDoc.IsActive)
                    throw new InvalidOperationException("Ya existe un usuario activo con ese documento de identidad.");

                // Inactivo → reactivar con los nuevos datos y resetear contraseña
                return await ReactivateUserAsync(existingByDoc, request);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username != null && u.Username.ToLower() == request.Username.Trim().ToLower());

            if (usernameExists)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
        }

        var status = request.Status ?? "Activo";

        // Los primeros 4 dígitos de la cédula son la contraseña inicial
        var docDigits = string.IsNullOrWhiteSpace(request.DocumentId)
            ? ""
            : new string(request.DocumentId.Trim().Where(char.IsDigit).Take(4).ToArray());
        var password = docDigits.Length >= 4 ? docDigits : "Diam2026!";

        var user = new User
        {
            FirstName          = request.FirstName.Trim(),
            LastName           = request.LastName.Trim(),
            Name               = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Username           = request.Username?.Trim(),
            Email              = request.Email.Trim().ToLower(),
            Phone              = request.Phone?.Trim(),
            DocumentId         = request.DocumentId?.Trim(),
            Role               = request.Role.Trim(),
            Status             = status,
            IsActive           = status == "Activo",
            PasswordHash       = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            MustChangePassword = true,
            CreatedAt          = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Asignar certificados
        if (request.Certificates.Count > 0)
        {
            var assignedAt = DateTime.UtcNow;
            foreach (var certId in request.Certificates.Distinct())
            {
                _context.UserCertificates.Add(new UserCertificate
                {
                    UserId        = user.Id,
                    CertificateId = certId,
                    AssignedAt    = assignedAt,
                });
            }
            await _context.SaveChangesAsync();
        }

        await SendWelcomeEmailSafe(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
            user.Username ?? user.Email.Split('@')[0], password);

        // Recargar con certificados para la respuesta
        var created = await _context.Users
            .Include(u => u.UserCertificates).ThenInclude(uc => uc.Certificate)
            .FirstAsync(u => u.Id == user.Id);

        return MapToResponse(created);
    }

    // ── REACTIVAR usuario inactivo con datos nuevos y contraseña reseteada ────
    private async Task<UserResponse> ReactivateUserAsync(User user, CreateUserRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameConflict = await _context.Users
                .AnyAsync(u => u.Id != user.Id && u.Username != null
                    && u.Username.ToLower() == request.Username.Trim().ToLower());

            if (usernameConflict)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
        }

        var docDigits = string.IsNullOrWhiteSpace(request.DocumentId)
            ? ""
            : new string(request.DocumentId.Trim().Where(char.IsDigit).Take(4).ToArray());
        var password = docDigits.Length >= 4 ? docDigits : "Diam2026!";

        user.FirstName          = request.FirstName.Trim();
        user.LastName           = request.LastName.Trim();
        user.Name               = $"{request.FirstName.Trim()} {request.LastName.Trim()}";
        user.Username           = request.Username?.Trim();
        user.Email              = request.Email.Trim().ToLower();
        user.Phone              = request.Phone?.Trim();
        user.DocumentId         = request.DocumentId?.Trim();
        user.Role               = request.Role.Trim();
        user.Status             = "Activo";
        user.IsActive           = true;
        user.PasswordHash       = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        user.MustChangePassword = true;
        user.UpdatedAt          = DateTime.UtcNow;

        // Reemplazar certificados
        var existingCerts = await _context.UserCertificates
            .Where(uc => uc.UserId == user.Id)
            .ToListAsync();
        _context.UserCertificates.RemoveRange(existingCerts);

        if (request.Certificates.Count > 0)
        {
            var assignedAt = DateTime.UtcNow;
            foreach (var certId in request.Certificates.Distinct())
            {
                _context.UserCertificates.Add(new UserCertificate
                {
                    UserId        = user.Id,
                    CertificateId = certId,
                    AssignedAt    = assignedAt,
                });
            }
        }

        await _context.SaveChangesAsync();

        await SendWelcomeEmailSafe(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
            user.Username ?? user.Email.Split('@')[0], password);

        var reactivated = await _context.Users
            .Include(u => u.UserCertificates).ThenInclude(uc => uc.Certificate)
            .FirstAsync(u => u.Id == user.Id);

        return MapToResponse(reactivated);
    }

    private async Task SendWelcomeEmailSafe(string email, string fullName, string username, string password)
    {
        try
        {
            await _emailService.SendWelcomeEmailAsync(email, fullName, username, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar correo de bienvenida a {Email}. El usuario fue creado correctamente.", email);
        }
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────
    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        if (request.Email is not null)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Id != id && u.Email.ToLower() == request.Email.Trim().ToLower());

            if (emailExists)
                throw new InvalidOperationException("Ya existe un usuario con ese correo electrónico.");
        }

        if (request.Username is not null && !string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Id != id && u.Username != null && u.Username.ToLower() == request.Username.Trim().ToLower());

            if (usernameExists)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
        }

        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName  is not null) user.LastName  = request.LastName.Trim();
        if (request.Email     is not null) user.Email     = request.Email.Trim().ToLower();
        if (request.Phone     is not null) user.Phone     = request.Phone.Trim();
        if (request.Username  is not null) user.Username  = request.Username.Trim();
        if (request.Role      is not null) user.Role      = request.Role.Trim();

        user.Name = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
        if (string.IsNullOrEmpty(user.Name)) user.Name = user.Email;

        if (request.Status is not null)
        {
            user.Status   = request.Status;
            user.IsActive = request.Status == "Activo";
        }

        // Reemplazar certificados si vienen en la solicitud
        if (request.Certificates is not null)
        {
            var existing = await _context.UserCertificates
                .Where(uc => uc.UserId == id)
                .ToListAsync();
            _context.UserCertificates.RemoveRange(existing);

            foreach (var certId in request.Certificates.Distinct())
            {
                _context.UserCertificates.Add(new UserCertificate
                {
                    UserId        = id,
                    CertificateId = certId,
                    AssignedAt    = DateTime.UtcNow,
                });
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Recargar con certificados para la respuesta
        var updated = await _context.Users
            .Include(u => u.UserCertificates).ThenInclude(uc => uc.Certificate)
            .FirstAsync(u => u.Id == id);

        return MapToResponse(updated);
    }

    // ── DELETE (bloquea eliminación de admin y auto-eliminación) ────────────
    public async Task<bool> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new InvalidOperationException("No puedes eliminarte a ti mismo.");

        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    // ── STATS ─────────────────────────────────────────────────────────────────
    public async Task<UserStatsResponse> GetStatsAsync()
    {
        var total  = await _context.Users.CountAsync();
        var active = await _context.Users.CountAsync(u => u.Status == "Activo");

        return new UserStatsResponse
        {
            Total         = total,
            ActiveCount   = active,
            InactiveCount = total - active,
        };
    }

    public async Task<List<AssignableRoleResponse>> GetAssignableRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive && r.DeletedAt == null)
            .OrderBy(r => r.Name)
            .Select(r => new AssignableRoleResponse(r.Id, r.Name))
            .ToListAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static UserResponse MapToResponse(User user)
    {
        var firstName = user.FirstName;
        var lastName  = user.LastName;

        if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(user.Name))
        {
            var parts = user.Name.Trim().Split(' ', 2);
            firstName = parts[0];
            lastName  = parts.Length > 1 ? parts[1] : "";
        }

        return new UserResponse
        {
            Id           = user.Id,
            FirstName    = firstName ?? "",
            LastName     = lastName ?? "",
            Username     = user.Username ?? user.Email.Split('@')[0],
            Email        = user.Email,
            Phone        = user.Phone,
            DocumentId   = user.DocumentId,
            Role         = user.Role,
            Status       = user.Status,
            Certificates = user.UserCertificates
                .Where(uc => uc.Certificate is not null)
                .Select(uc => new CertificateResponse(uc.CertificateId, uc.Certificate!.Name))
                .ToList(),
            CreatedAt    = user.CreatedAt,
            UpdatedAt    = user.UpdatedAt,
        };
    }
}
