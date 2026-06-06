using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints
{
    public static class LevelEndpoints
    {
        public static void MapLevelEndpoints(this WebApplication app)
        {
            app.MapGet("/levels", async (ILevelRepository repository) =>
                Results.Ok(await repository.GetAllWithResultsAsync()));

            app.MapGet("/levels/{id:int}", async (int id, ILevelRepository repository) =>
                await EndpointResponseHelper.GetByIdAsync(id, repository.GetByIdWithResultsAsync));

            app.MapPost("/levels", async (CreateLevelDto dto, ILevelRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var level = new Level
                {
                    Name = dto.Name,
                    Difficulty = dto.Difficulty
                };

                var created = await repository.AddAsync(level);
                return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdLevel, "levels", repository.GetByIdWithResultsAsync);
            });

            app.MapPut("/levels/{id:int}", async (int id, UpdateLevelDto dto, ILevelRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var existingLevel = await repository.GetByIdAsync(id);
                if (existingLevel is null)
                {
                    return Results.NotFound();
                }

                existingLevel.Name = dto.Name;
                existingLevel.Difficulty = dto.Difficulty;

                await repository.UpdateAsync(existingLevel);
                return await EndpointResponseHelper.UpdateWithDetailsAsync(id, repository.GetByIdWithResultsAsync);
            });

            app.MapDelete("/levels/{id:int}", async (int id, ILevelRepository repository) =>
                EndpointResponseHelper.DeleteResult(await repository.DeleteAsync(id)));
        }
    }
}
