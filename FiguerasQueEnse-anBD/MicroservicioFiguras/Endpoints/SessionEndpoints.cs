using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints
{
    public static class SessionEndpoints
    {
        public static void MapSessionEndpoints(this WebApplication app)
        {
            app.MapGet("/tutors/{tutorId:int}/sessions", async (int tutorId, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();

                if (role != "tutor" || !userId.HasValue || userId.Value != tutorId)
                {
                    return Results.Forbid();
                }

                var studentIds = await studentRepository.GetStudentIdsByTutorAsync(tutorId);
                var all = await sessionRepository.GetAllWithRelationsAsync();
                var filtered = all.Where(s => studentIds.Contains(s.IdStudent)).ToList();
                return Results.Ok(filtered);
            });

            app.MapGet("/students/{studentId:int}/sessions", async (int studentId, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();

                if (!userId.HasValue)
                {
                    return Results.Forbid();
                }

                if (role == "student")
                {
                    if (userId.Value != studentId)
                    {
                        return Results.Forbid();
                    }

                    var all = await sessionRepository.GetAllWithRelationsAsync();
                    return Results.Ok(all.Where(s => s.IdStudent == studentId).ToList());
                }

                if (role == "tutor")
                {
                    var assigned = await studentRepository.IsStudentAssignedToTutorAsync(studentId, userId.Value);
                    if (!assigned)
                    {
                        return Results.Forbid();
                    }

                    var all = await sessionRepository.GetAllWithRelationsAsync();
                    return Results.Ok(all.Where(s => s.IdStudent == studentId).ToList());
                }

                return Results.Forbid();
            });


            app.MapGet("/sessions", async (HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (!userId.HasValue)
                    return Results.Forbid();

                if (role == "student")
                {
                    // Solo sus propias sesiones
                    var all = await sessionRepository.GetAllWithRelationsAsync();
                    var filtered = all.Where(s => s.IdStudent == userId.Value).ToList();
                    return Results.Ok(filtered);
                }
                if (role == "tutor")
                {
                    // Solo sesiones de sus estudiantes
                    var studentIds = await studentRepository.GetStudentIdsByTutorAsync(userId.Value);
                    var all = await sessionRepository.GetAllWithRelationsAsync();
                    var filtered = all.Where(s => studentIds.Contains(s.IdStudent)).ToList();
                    return Results.Ok(filtered);
                }
                return Results.Forbid();
            });


            app.MapGet("/sessions/{id:int}", async (int id, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var session = await sessionRepository.GetByIdWithRelationsAsync(id);
                if (session == null)
                    return Results.NotFound();

                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (!userId.HasValue)
                    return Results.Forbid();

                if (role == "student" && session.IdStudent == userId.Value)
                    return Results.Ok(session);
                if (role == "tutor")
                {
                    if (await studentRepository.IsStudentAssignedToTutorAsync(session.IdStudent, userId.Value))
                        return Results.Ok(session);
                }
                return Results.Forbid();
            });


            app.MapPost("/sessions", async (CreateSessionDto dto, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (!userId.HasValue)
                    return Results.Forbid();

                if (role == "student" && dto.IdStudent != userId.Value)
                    return Results.Forbid();
                if (role == "tutor")
                {
                    if (!await studentRepository.IsStudentAssignedToTutorAsync(dto.IdStudent, userId.Value))
                        return Results.Forbid();
                }
                else if (role != "student" && role != "tutor")
                {
                    return Results.Forbid();
                }

                var session = new Session
                {
                    IdStudent = dto.IdStudent,
                    BeginningDate = dto.BeginningDate,
                    // EndDate lo gestiona el backend cuando llegan level-results.
                    EndDate = null,
                    Device = dto.Device
                };

                var created = await sessionRepository.AddAsync(session);
                return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdSession, "sessions", sessionRepository.GetByIdWithRelationsAsync);
            });


            app.MapPut("/sessions/{id:int}", async (int id, UpdateSessionDto dto, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var existingSession = await sessionRepository.GetByIdAsync(id);
                if (existingSession is null)
                {
                    return Results.NotFound();
                }

                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (!userId.HasValue)
                    return Results.Forbid();

                if (role == "student" && existingSession.IdStudent != userId.Value)
                    return Results.Forbid();
                if (role == "tutor")
                {
                    if (!await studentRepository.IsStudentAssignedToTutorAsync(existingSession.IdStudent, userId.Value))
                        return Results.Forbid();

                    if (!await studentRepository.IsStudentAssignedToTutorAsync(dto.IdStudent, userId.Value))
                        return Results.Forbid();
                }
                else if (role != "student" && role != "tutor")
                {
                    return Results.Forbid();
                }

                existingSession.IdStudent = dto.IdStudent;
                existingSession.BeginningDate = dto.BeginningDate;
                existingSession.EndDate = dto.EndDate;
                existingSession.Device = dto.Device;

                await sessionRepository.UpdateAsync(existingSession);
                return await EndpointResponseHelper.UpdateWithDetailsAsync(id, sessionRepository.GetByIdWithRelationsAsync);
            });


            app.MapDelete("/sessions/{id:int}", async (int id, HttpContext http, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var session = await sessionRepository.GetByIdAsync(id);
                if (session == null)
                    return Results.NotFound();

                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();
                if (!userId.HasValue)
                    return Results.Forbid();

                if (role == "student" && session.IdStudent != userId.Value)
                    return Results.Forbid();
                if (role == "tutor")
                {
                    if (!await studentRepository.IsStudentAssignedToTutorAsync(session.IdStudent, userId.Value))
                        return Results.Forbid();
                }
                else if (role != "student" && role != "tutor")
                {
                    return Results.Forbid();
                }

                return EndpointResponseHelper.DeleteResult(await sessionRepository.DeleteAsync(id));
            });
        }
    }
}
