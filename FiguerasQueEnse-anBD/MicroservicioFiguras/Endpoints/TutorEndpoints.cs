using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints
{
    public static class TutorEndpoints
    {
        public static void MapTutorEndpoints(this WebApplication app)
        {
            app.MapGet("/tutors", async (HttpContext http, ITutorRepository repository) =>
                await GetTutorForCurrentUserAsync(http, repository));

            app.MapGet("/tutors/{id:int}", async (HttpContext http, int id, ITutorRepository repository) =>
            {
                var authorizationFailure = ValidateTutorSelfAccess(http, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                return await EndpointResponseHelper.GetByIdAsync(id, repository.GetByIdWithStudentsAsync);
            });

            app.MapGet("/tutors/{id:int}/students", async (HttpContext http, int id, IStudentRepository studentRepository, ITutorRepository tutorRepository) =>
            {
                var authorizationFailure = ValidateTutorSelfAccess(http, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                var tutor = await tutorRepository.GetByIdWithStudentsAsync(id);
                if (tutor is null) return Results.NotFound();
                var students = await studentRepository.GetStudentsByTutorIdAsync(id);
                return Results.Ok(students);
            });

            app.MapPost("/tutors", async (CreateTutorDto dto, ITutorRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var tutor = new Tutor
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PasswordHash = dto.PasswordHash,
                    Country = dto.Country,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    Degree = dto.Degree,
                    RegistrationDate = DateTime.Now
                };

                var created = await repository.AddAsync(tutor);
                return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdTutor, "tutors", repository.GetByIdWithStudentsAsync);
            });

            app.MapPost("/tutors/assign-student", async (HttpContext http, AssignTutorDto dto, IStudentRepository repository, ITutorRepository tutorRepository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (role != "tutor" || !userId.HasValue)
                {
                    return Results.Forbid();
                }

                var currentTutor = await tutorRepository.GetByIdAsync(userId.Value);
                if (currentTutor is null)
                {
                    return Results.Forbid();
                }

                if (!string.Equals(currentTutor.Email, dto.TutorEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Forbid();
                }

                var result = await repository.AssignTutorByEmailAsync(dto.StudentEmail, dto.TutorEmail);
                return result switch
                {
                    AssignTutorByEmailResult.Success => Results.Ok(),
                    AssignTutorByEmailResult.StudentNotFound => Results.NotFound(new { message = "No existe un alumno con ese correo." }),
                    AssignTutorByEmailResult.StudentEmailBelongsToTutor => Results.NotFound(new { message = "Ese correo pertenece a un tutor, no a un alumno." }),
                    AssignTutorByEmailResult.TutorNotFound => Results.NotFound(new { message = "No existe un tutor con ese correo." }),
                    AssignTutorByEmailResult.StudentAlreadyAssignedToCurrentTutor => Results.Conflict(new { message = "Ese alumno ya esta asignado a tu perfil." }),
                    AssignTutorByEmailResult.StudentAlreadyAssignedToAnotherTutor => Results.Conflict(new { message = "Ese alumno ya tiene un tutor asignado." }),
                    _ => Results.BadRequest(new { message = "No se pudo asignar el alumno." })
                };
            });

            app.MapPut("/tutors/{id:int}", async (HttpContext http, int id, UpdateTutorDto dto, ITutorRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var authorizationFailure = ValidateTutorSelfAccess(http, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                var existingTutor = await repository.GetByIdAsync(id);
                if (existingTutor is null)
                {
                    return Results.NotFound();
                }

                existingTutor.Email = dto.Email;
                existingTutor.Name = dto.Name;
                existingTutor.Country = dto.Country;
                existingTutor.Gender = dto.Gender;
                existingTutor.Age = dto.Age;
                existingTutor.Degree = dto.Degree;

                await repository.UpdateAsync(existingTutor);
                return await EndpointResponseHelper.UpdateWithDetailsAsync(id, repository.GetByIdWithStudentsAsync);
            });

            app.MapDelete("/tutors/{id:int}", async (HttpContext http, int id, ITutorRepository repository) =>
            {
                var authorizationFailure = ValidateTutorSelfAccess(http, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                return EndpointResponseHelper.DeleteResult(await repository.DeleteAsync(id));
            });
        }

        private static async Task<IResult> GetTutorForCurrentUserAsync(HttpContext http, ITutorRepository repository)
        {
            var userId = http.User.GetUserId();
            var role = http.User.GetUserRole();

            if (role != "tutor" || !userId.HasValue)
            {
                return Results.Forbid();
            }

            var tutor = await repository.GetByIdWithStudentsAsync(userId.Value);
            return tutor is not null ? Results.Ok(new[] { tutor }) : Results.NotFound();
        }

        private static IResult? ValidateTutorSelfAccess(HttpContext http, int tutorId)
        {
            var userId = http.User.GetUserId();
            var role = http.User.GetUserRole();

            if (role != "tutor" || !userId.HasValue || userId.Value != tutorId)
            {
                return Results.Forbid();
            }

            return null;
        }
    }
}
