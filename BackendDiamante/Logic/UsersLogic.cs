using System.Text.Json;
using BackendDiamante.Data;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.DTOs.Users;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Logic;

public class UsersLogic : IUsersLogic
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsersLogic> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Roles protegidos que no se pueden asignar, editar ni eliminar desde el modulo Usuarios.</summary>
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

    /// <summary>Determina si un usuario tiene un rol protegido.</summary>
    private static bool IsProtectedUser(User user) =>
        ProtectedRoles.Contains(user.Role);

    /// <summary>Determina si un rol es protegido.</summary>
    private static bool IsProtectedRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) && ProtectedRoles.Contains(role.Trim());

    // ── GET ALL (excluye usuarios admin y al usuario actual) ───────────────
    public async Task<List<UserResponse>> GetAllAsync(int currentUserId)
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return users
            .Where(u => !IsProtectedUser(u) && u.Id != currentUserId)
            .Select(MapToResponse)
            .ToList();
    }

    // ── GET BY ID (bloquea admin) ────────────────────────────────────────────
    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null || IsProtectedUser(user)) return null;
        return MapToResponse(user);
    }

    // ── CREATE (bloquea asignacion de rol admin) ─────────────────────────────
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // Validar que no se asigne un rol protegido
        if (IsProtectedRole(request.Role))
            throw new InvalidOperationException("No es posible asignar el rol de administrador.");

        // Validar email unico
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

        if (emailExists)
            throw new InvalidOperationException("Ya existe un usuario con ese correo electronico.");

        // Validar username unico (si viene)
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username != null && u.Username.ToLower() == request.Username.Trim().ToLower());

            if (usernameExists)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
        }

        // Validar documentId unico (si viene)
        if (!string.IsNullOrWhiteSpace(request.DocumentId))
        {
            var docExists = await _context.Users
                .AnyAsync(u => u.DocumentId != null && u.DocumentId == request.DocumentId.Trim());

            if (docExists)
                throw new InvalidOperationException("Ya existe un usuario con ese documento de identidad.");
        }

        var status = request.Status ?? "Activo";

        // La cedula se usa como contrasena inicial
        var password = !string.IsNullOrWhiteSpace(request.DocumentId)
            ? request.DocumentId.Trim()
            : "Diamante2026!"; // Contrasena por defecto si no hay cedula

        var user = new User
        {
            FirstName    = request.FirstName.Trim(),
            LastName     = request.LastName.Trim(),
            Name         = $"{request.FirstName.Trim()} {request.LastName.Trim()}", // compatibilidad auth
            Username     = request.Username?.Trim(),
            Email        = request.Email.Trim().ToLower(),
            Phone        = request.Phone?.Trim(),
            DocumentId   = request.DocumentId?.Trim(),
            Role         = request.Role.Trim(),
            Status       = status,
            IsActive     = status == "Activo",
            PasswordHash       = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            MustChangePassword = true,
            Certificates       = SerializeCertificates(request.Certificates),
            CreatedAt          = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        try
        {
            var displayUsername = user.Username ?? user.Email.Split('@')[0];
            await _emailService.SendWelcomeEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                displayUsername,
                password
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar correo de bienvenida a {Email}. El usuario fue creado correctamente.", user.Email);
        }

        return MapToResponse(user);
    }

    // ── UPDATE (bloquea edicion de admin y asignacion de rol admin) ───────────
    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        // Bloquear edicion de usuarios con rol protegido
        if (IsProtectedUser(user))
            throw new InvalidOperationException("No es posible modificar un usuario administrador.");

        // Bloquear asignacion de rol protegido
        if (IsProtectedRole(request.Role))
            throw new InvalidOperationException("No es posible asignar el rol de administrador.");

        // Validar email unico (si cambia)
        if (request.Email is not null)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Id != id && u.Email.ToLower() == request.Email.Trim().ToLower());

            if (emailExists)
                throw new InvalidOperationException("Ya existe un usuario con ese correo electronico.");
        }

        // Validar username unico (si cambia)
        if (request.Username is not null && !string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Id != id && u.Username != null && u.Username.ToLower() == request.Username.Trim().ToLower());

            if (usernameExists)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
        }

        // Aplicar cambios
        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName  is not null) user.LastName  = request.LastName.Trim();
        if (request.Email     is not null) user.Email     = request.Email.Trim().ToLower();
        if (request.Phone     is not null) user.Phone     = request.Phone.Trim();
        if (request.Username  is not null) user.Username  = request.Username.Trim();
        if (request.Role      is not null) user.Role      = request.Role.Trim();

        if (request.Certificates is not null)
            user.Certificates = SerializeCertificates(request.Certificates);

        // Sincronizar Name (compatibilidad auth)
        user.Name = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
        if (string.IsNullOrEmpty(user.Name)) user.Name = user.Email;

        // Sincronizar Status <-> IsActive
        if (request.Status is not null)
        {
            user.Status   = request.Status;
            user.IsActive = request.Status == "Activo";
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(user);
    }

    // ── DELETE (bloquea eliminacion de admin y auto-eliminacion) ────────────
    public async Task<bool> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new InvalidOperationException("No puedes eliminarte a ti mismo.");

        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        // Bloquear eliminacion de usuarios con rol protegido
        if (IsProtectedUser(user))
            throw new InvalidOperationException("No es posible eliminar un usuario administrador.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    // ── STATS (excluye admin del conteo) ─────────────────────────────────────
    public async Task<UserStatsResponse> GetStatsAsync()
    {
        var nonAdminQuery = _context.Users
            .Where(u => !ProtectedRoles.Contains(u.Role));

        var total  = await nonAdminQuery.CountAsync();
        var active = await nonAdminQuery.CountAsync(u => u.Status == "Activo");

        return new UserStatsResponse
        {
            Total         = total,
            ActiveCount   = active,
            InactiveCount = total - active,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? SerializeCertificates(List<string>? certs)
    {
        if (certs is null || certs.Count == 0) return null;
        return JsonSerializer.Serialize(certs);
    }

    private static List<string> DeserializeCertificates(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? []; }
        catch { return []; }
    }

    private static UserResponse MapToResponse(User user)
    {
        // Backward compat: si FirstName es null, inferir de Name
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
            Certificates = DeserializeCertificates(user.Certificates),
            CreatedAt    = user.CreatedAt,
            UpdatedAt    = user.UpdatedAt,
        };
    }
}
