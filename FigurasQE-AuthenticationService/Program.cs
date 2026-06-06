using FigurasQE_AuthenticationService.Services;
using FigurasQE_AuthenticationService.Data;
using FigurasQE_AuthenticationService.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FigurasQE Authentication Service API",
        Version = "v1",
        Description = "API de autenticacion para login, registro y emision de JWT."
    });
});
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();

builder.Services.AddDbContext<FigurasqeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

//app.UseHttpsRedirection();
app.MapGet("/health", async (FigurasqeContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    return canConnect
        ? Results.Ok(new { service = "auth", status = "ok", database = "ok" })
        : Results.Json(new { service = "auth", status = "down", database = "unavailable" }, statusCode: 503);
});

app.MapControllers();

app.Run();