using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MicroservicioFiguras.Helpers;

public static class EndpointResponseHelper
{
    public static bool TryValidateDto<T>(T? dto, out IResult? validationError)
    {
        if (DtoValidationHelper.TryValidate(dto, out var errors))
        {
            validationError = null;
            return true;
        }

        validationError = Results.BadRequest(new { errors });
        return false;
    }

    public static async Task<IResult> GetByIdAsync<TDto>(int id, Func<int, Task<TDto?>> loadDtoAsync)
    {
        var dto = await loadDtoAsync(id);
        return dto is not null ? Results.Ok(dto) : Results.NotFound();
    }

    public static async Task<IResult> CreateWithDetailsAsync<TDto>(int createdId, string routePrefix, Func<int, Task<TDto?>> loadDtoAsync)
    {
        var createdDto = await loadDtoAsync(createdId);
        return createdDto is not null ? Results.Created($"/{routePrefix}/{createdId}", createdDto) : Results.BadRequest();
    }

    public static async Task<IResult> UpdateWithDetailsAsync<TDto>(int id, Func<int, Task<TDto?>> loadDtoAsync)
    {
        var updatedDto = await loadDtoAsync(id);
        return updatedDto is not null ? Results.Ok(updatedDto) : Results.BadRequest();
    }

    public static IResult DeleteResult(bool deleted)
    {
        return deleted ? Results.Ok() : Results.NotFound();
    }
}