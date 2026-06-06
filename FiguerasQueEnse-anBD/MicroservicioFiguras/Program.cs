using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;
using MicroservicioFiguras.Repositories;
using MicroservicioFiguras.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FigurasqeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured. Set Jwt:Key in appsettings.json or environment variables.");
}

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "admin"));

    options.AddPolicy("StudentOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "student"));

    options.AddPolicy("TutorOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "tutor"));

    options.AddPolicy("StudentOrTutor", policy =>
        policy.RequireClaim(ClaimTypes.Role, new[] { "student", "tutor" }));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<ILevelRepository, LevelRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ILevelResultRepository, LevelResultRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// Leer puerto desde appsettings.json
var servicePort = builder.Configuration.GetValue<int?>("Service:Port") ?? 5124;
builder.WebHost.UseUrls($"http://*:{servicePort}");

// Leer endpoints externos desde appsettings.json
var authServiceUrl = builder.Configuration["Service:Endpoints:AuthService"] ?? "http://localhost:5041";
var logsServiceUrl = builder.Configuration["Service:Endpoints:LogsService"] ?? "http://localhost:5186";

var app = builder.Build();

await EnsureRequiredLevelsAsync(app.Services);

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (FigurasqeContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    return canConnect
        ? Results.Ok(new { service = "data", status = "ok", database = "ok" })
        : Results.Json(new { service = "data", status = "down", database = "unavailable" }, statusCode: 503);
}).AllowAnonymous();

app.MapAdminEndpoints();
app.MapDashboardEndpoints();
app.MapStudentEndpoints();
app.MapTutorEndpoints();
app.MapLevelEndpoints();
app.MapSessionEndpoints();
app.MapLevelResultEndpoints();

app.Run();

static async Task EnsureRequiredLevelsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FigurasqeContext>();

    if (await db.Levels.AnyAsync(level => level.IdLevel == 6))
    {
        return;
    }

    db.Levels.Add(new Level
    {
        IdLevel = 6,
        Name = "Master",
        Difficulty = 12
    });

    await db.SaveChangesAsync();
    await db.Database.ExecuteSqlRawAsync(
        "SELECT setval(pg_get_serial_sequence('levels', 'id_level'), (SELECT MAX(id_level) FROM levels));");
}
