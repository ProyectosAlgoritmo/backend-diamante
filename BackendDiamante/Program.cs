using System.Text;
using System.Threading.RateLimiting;
using BackendDiamante.Data;
using BackendDiamante.Logic;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BackendDiamante API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa el token JWT. Ejemplo: Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ─── Base de datos ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ─── Rate Limiting (protección brute force en login) ─────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit            = 5;
        limiterOptions.Window                 = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit             = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"message":"Demasiados intentos. Espera 1 minuto e intenta de nuevo."}""");
    };
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ─── HttpClient (para validación de tokens Google vía userinfo endpoint) ──────
builder.Services.AddHttpClient();

// ─── Servicios de negocio ─────────────────────────────────────────────────────
builder.Services.AddScoped<IRolesLogic,       RolesLogic>();
builder.Services.AddScoped<IModulesLogic,     ModulesLogic>();
builder.Services.AddScoped<IAuthLogic,        AuthLogic>();
builder.Services.AddScoped<ICostCentersLogic, CostCentersLogic>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ─── Seed usuarios por defecto (solo si no existen) ──────────────────────────
await SeedDefaultUsersAsync(app);

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS solo en producción — localmente no hay certificado configurado
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ─── Seed Helper ─────────────────────────────────────────────────────────────
static async Task SeedDefaultUsersAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                new() { Email = "admin@diamante.co",      Name = "Administrador Diamante", Role = "admin",      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")   },
                new() { Email = "supervisor@diamante.co", Name = "Supervisor Diamante",    Role = "supervisor", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super123!")   },
                new() { Email = "cliente@diamante.co",    Name = "Cliente Diamante",       Role = "cliente",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!") },
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ Usuarios de prueba creados: admin, supervisor, cliente");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error durante el seed de usuarios");
    }
}
