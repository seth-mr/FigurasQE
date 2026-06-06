using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints
{
    public static class StudentEndpoints
    {
        public static void MapStudentEndpoints(this WebApplication app)
        {
            app.MapGet("/students", async (HttpContext http, IStudentRepository repository) =>
                await GetStudentsForCurrentUserAsync(http, repository));

            app.MapGet("/students/{id:int}", async (HttpContext http, int id, IStudentRepository repository) =>
            {
                var authorizationFailure = await ValidateStudentAccessAsync(http, repository, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                var student = await repository.GetByIdWithTutorAsync(id);
                return student is not null ? Results.Ok(student) : Results.NotFound();
            });

            app.MapGet("/students/{id:int}/tutor", async (HttpContext http, int id, IStudentRepository repository) =>
            {
                var authorizationFailure = await ValidateStudentAccessAsync(http, repository, id);
                if (authorizationFailure is not null) return authorizationFailure;

                var student = await repository.GetByIdWithTutorAsync(id);
                if (student is null) return Results.NotFound();
                return student.Tutor is not null ? Results.Ok(student.Tutor) : Results.NotFound();
            });

            app.MapPost("/students", async (CreateStudentDto dto, IStudentRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                if (await repository.IsEmailTakenAsync(dto.Email!))
                {
                    return Results.BadRequest(new { errors = new[] { "Email is already registered." } });
                }

                if (dto.IdTutor.HasValue && !await repository.TutorExistsAsync(dto.IdTutor.Value))
                {
                    return Results.BadRequest(new { errors = new[] { "IdTutor does not exist." } });
                }

                var student = new Student
                {
                    IdTutor = dto.IdTutor,
                    Name = dto.Name,
                    Email = dto.Email,
                    PasswordHash = dto.PasswordHash,
                    Age = dto.Age,
                    Gender = dto.Gender,
                    Country = dto.Country,
                    Neurodivergency = dto.Neurodivergency,
                    RegistrationDate = DateTime.Now
                };

                var created = await repository.AddAsync(student);
                return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdStudent, "students", repository.GetByIdWithTutorAsync);
            });

            app.MapPut("/students/{id:int}", async (HttpContext http, int id, UpdateStudentDto dto, IStudentRepository repository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var authorizationFailure = await ValidateStudentAccessAsync(http, repository, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                var existingStudent = await repository.GetByIdAsync(id);
                if (existingStudent is null)
                {
                    return Results.NotFound();
                }

                if (await repository.IsEmailTakenByOtherAsync(id, dto.Email!))
                {
                    return Results.BadRequest(new { errors = new[] { "Email is already registered by another student." } });
                }

                if (dto.IdTutor.HasValue && !await repository.TutorExistsAsync(dto.IdTutor.Value))
                {
                    return Results.BadRequest(new { errors = new[] { "IdTutor does not exist." } });
                }

                existingStudent.IdTutor = dto.IdTutor;
                existingStudent.Name = dto.Name;
                existingStudent.Email = dto.Email;
                existingStudent.Age = dto.Age;
                existingStudent.Gender = dto.Gender;
                existingStudent.Country = dto.Country;
                existingStudent.Neurodivergency = dto.Neurodivergency;

                await repository.UpdateAsync(existingStudent);
                return await EndpointResponseHelper.UpdateWithDetailsAsync(id, repository.GetByIdWithTutorAsync);
            });

            app.MapDelete("/students/{id:int}", async (HttpContext http, int id, IStudentRepository repository) =>
            {
                var authorizationFailure = await ValidateStudentAccessAsync(http, repository, id);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                return EndpointResponseHelper.DeleteResult(await repository.DeleteAsync(id));
            });

            app.MapGet("/students/tutor/{tutorId:int}/ids", async (HttpContext http, int tutorId, IStudentRepository repository) =>
            {
                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();

                if (role != "tutor" || !userId.HasValue || userId.Value != tutorId)
                {
                    return Results.Forbid();
                }

                var studentIds = await repository.GetStudentIdsByTutorAsync(tutorId);
                return Results.Ok(studentIds);
            });

            app.MapGet("/students/{studentId:int}/sessions/ids", async (HttpContext http, int studentId, IStudentRepository repository) =>
            {
                var authorizationFailure = await ValidateStudentAccessAsync(http, repository, studentId);
                if (authorizationFailure is not null)
                {
                    return authorizationFailure;
                }

                var sessionIds = await repository.GetSessionIdsByStudentAsync(studentId);
                return Results.Ok(sessionIds);
            });
        }

        private static async Task<IResult> GetStudentsForCurrentUserAsync(HttpContext http, IStudentRepository repository)
        {
            var userId = http.User.GetUserId();
            var role = http.User.GetUserRole();

            if (!userId.HasValue)
            {
                return Results.Forbid();
            }

            if (role == "student")
            {
                var student = await repository.GetByIdWithTutorAsync(userId.Value);
                return student is not null ? Results.Ok(new[] { student }) : Results.NotFound();
            }

            if (role == "tutor")
            {
                var students = await repository.GetStudentsByTutorIdAsync(userId.Value);
                return Results.Ok(students);
            }

            return Results.Forbid();
        }

        private static async Task<IResult?> ValidateStudentAccessAsync(HttpContext http, IStudentRepository repository, int studentId)
        {
            var userId = http.User.GetUserId();
            var role = http.User.GetUserRole();

            if (role == "student")
            {
                return userId == studentId ? null : Results.Forbid();
            }

            if (role == "tutor")
            {
                if (!userId.HasValue || !await repository.IsStudentAssignedToTutorAsync(studentId, userId.Value))
                {
                    return Results.Forbid();
                }

                return null;
            }

            return Results.Forbid();
        }
    }
}
