using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using BackendDiamante.Data;
using BackendDiamante.Data.Seeders;
using BackendDiamante.Logic;
using BackendDiamante.Logic.Interfaces;
using BackendDiamante.Middleware;
using BackendDiamante.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var body = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Tu sesion no es valida o expiro."
                });

                await context.Response.WriteAsync(body);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var body = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "No tienes permisos para realizar esta accion."
                });

                await context.Response.WriteAsync(body);
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"message":"Demasiados intentos. Espera 1 minuto e intenta de nuevo."}""");
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<IRolesLogic, RolesLogic>();
builder.Services.AddScoped<IModulesLogic, ModulesLogic>();
builder.Services.AddScoped<IAuthLogic, AuthLogic>();
builder.Services.AddScoped<IUsersLogic, UsersLogic>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICostCentersLogic, CostCentersLogic>();
builder.Services.AddScoped<INotificationsLogic, NotificationsLogic>();

var app = builder.Build();

await SeedStartupDataAsync(app);

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PermissionAuthorizationMiddleware>();
app.MapControllers();

app.Run();

static async Task SeedStartupDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        await SecurityModulesSeed.SeedAsync(context, logger);
        await SecurityRolesSeed.SeedAsync(context, logger);
        await SeedDefaultUsersAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante el seed de arranque");
    }
}

static async Task SeedDefaultUsersAsync(ApplicationDbContext context, ILogger logger)
{
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

        logger.LogInformation("Usuarios de prueba creados: admin, supervisor, cliente");
    }
}
