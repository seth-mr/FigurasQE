using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapPost("/admins", async (HttpContext http, CreateAdminDto dto, IAdminRepository repository) =>
        {
            if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
            {
                return validationError;
            }

            var adminsExist = await repository.HasAnyAdminsAsync();
            if (adminsExist && http.User.GetUserRole() != "admin")
            {
                return Results.Forbid();
            }

            if (await repository.EmailExistsAsync(dto.Email))
            {
                return Results.Conflict(new { message = "An admin with that email already exists." });
            }

            if (await repository.UsernameExistsAsync(dto.Username))
            {
                return Results.Conflict(new { message = "An admin with that username already exists." });
            }

            var admin = new Admin
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RegistrationDate = DateTime.Now
            };

            var created = await repository.AddAsync(admin);
            return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdAdmin, "admins", repository.GetByIdDetailsAsync);
        })
        .AllowAnonymous();

        var group = app.MapGroup("/admins")
            .RequireAuthorization("AdminOnly");

        group.MapGet(string.Empty, async (IAdminRepository repository) =>
            Results.Ok(await repository.GetAllDetailsAsync()));

        group.MapGet("/{id:int}", async (int id, IAdminRepository repository) =>
            await EndpointResponseHelper.GetByIdAsync(id, repository.GetByIdDetailsAsync));

        group.MapPut("/{id:int}", async (int id, UpdateAdminDto dto, IAdminRepository repository) =>
        {
            if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
            {
                return validationError;
            }

            var existingAdmin = await repository.GetByIdAsync(id);
            if (existingAdmin is null)
            {
                return Results.NotFound();
            }

            if (await repository.EmailExistsAsync(dto.Email, id))
            {
                return Results.Conflict(new { message = "An admin with that email already exists." });
            }

            if (await repository.UsernameExistsAsync(dto.Username, id))
            {
                return Results.Conflict(new { message = "An admin with that username already exists." });
            }

            existingAdmin.Name = dto.Name;
            existingAdmin.Email = dto.Email;
            existingAdmin.Phone = dto.Phone;
            existingAdmin.Username = dto.Username;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await repository.UpdateAsync(existingAdmin);
            return await EndpointResponseHelper.UpdateWithDetailsAsync(id, repository.GetByIdDetailsAsync);
        });

        group.MapDelete("/{id:int}", async (int id, IAdminRepository repository) =>
            EndpointResponseHelper.DeleteResult(await repository.DeleteAsync(id)));
    }
}