using Microsoft.AspNetCore.Builder;
using System.Linq;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Helpers;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Endpoints
{
    public static class LevelResultEndpoints
    {
        public static void MapLevelResultEndpoints(this WebApplication app)
        {
            app.MapGet("/level-results", async (HttpContext http, ILevelResultRepository levelResultRepository, IStudentRepository studentRepository) =>
            {
                var userId = http.User.GetUserId();
                var role = http.User.GetUserRole();

                if (!userId.HasValue)
                {
                    return Results.Forbid();
                }

                var all = await levelResultRepository.GetAllWithRelationsAsync();

                if (role == "student")
                {
                    return Results.Ok(all.Where(r => r.Session?.IdStudent == userId.Value).ToList());
                }

                if (role == "tutor")
                {
                    var studentIds = await studentRepository.GetStudentIdsByTutorAsync(userId.Value);
                    return Results.Ok(all.Where(r => r.Session is not null && studentIds.Contains(r.Session.IdStudent)).ToList());
                }

                return Results.Forbid();
            });

            app.MapGet("/level-results/{id:int}", async (int id, HttpContext http, ILevelResultRepository levelResultRepository, IStudentRepository studentRepository) =>
            {
                var result = await levelResultRepository.GetByIdWithRelationsAsync(id);
                if (result is null)
                {
                    return Results.NotFound();
                }

                var accessValidation = await ValidateStudentAccessAsync(http, studentRepository, result.Session?.IdStudent);
                if (accessValidation is not null)
                {
                    return accessValidation;
                }

                return Results.Ok(result);
            });

            app.MapGet("/sessions/{sessionId:int}/level-results/ids", async (int sessionId, HttpContext http, ILevelResultRepository levelResultRepository, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var session = await sessionRepository.GetByIdAsync(sessionId);
                if (session is null)
                {
                    return Results.NotFound();
                }

                var accessValidation = await ValidateStudentAccessAsync(http, studentRepository, session.IdStudent);
                if (accessValidation is not null)
                {
                    return accessValidation;
                }

                var ids = await levelResultRepository.GetIdsBySessionAsync(sessionId);
                return Results.Ok(ids);
            });

            app.MapPost("/level-results", async (CreateLevelResultDto dto, HttpContext http, ILevelResultRepository levelResultRepository, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var session = await sessionRepository.GetByIdAsync(dto.IdSession);
                if (session is null)
                {
                    return Results.NotFound();
                }

                var accessValidation = await ValidateStudentAccessAsync(http, studentRepository, session.IdStudent);
                if (accessValidation is not null)
                {
                    return accessValidation;
                }

                var levelResult = new LevelResult
                {
                    IdSession = dto.IdSession,
                    IdLevel = dto.IdLevel,
                    FinishingTime = dto.FinishingTime,
                    Attempts = dto.Attempts,
                    Fails = dto.Fails,
                    Completed = dto.Completed
                };
                var receivedAtServerTime = DateTime.Now;

                var created = await levelResultRepository.AddAsync(levelResult);
                await sessionRepository.UpdateEndDateAsync(session.IdSession, receivedAtServerTime);
                return await EndpointResponseHelper.CreateWithDetailsAsync(created.IdResult, "level-results", levelResultRepository.GetByIdWithRelationsAsync);
            });

            app.MapPut("/level-results/{id:int}", async (int id, UpdateLevelResultDto dto, HttpContext http, ILevelResultRepository levelResultRepository, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                if (!EndpointResponseHelper.TryValidateDto(dto, out var validationError))
                {
                    return validationError;
                }

                var existingResult = await levelResultRepository.GetByIdAsync(id);
                if (existingResult is null)
                {
                    return Results.NotFound();
                }

                var existingSession = await sessionRepository.GetByIdAsync(existingResult.IdSession);
                if (existingSession is null)
                {
                    return Results.NotFound();
                }

                var targetSession = await sessionRepository.GetByIdAsync(dto.IdSession);
                if (targetSession is null)
                {
                    return Results.NotFound();
                }

                var currentAccessValidation = await ValidateStudentAccessAsync(http, studentRepository, existingSession.IdStudent);
                if (currentAccessValidation is not null)
                {
                    return currentAccessValidation;
                }

                var targetAccessValidation = await ValidateStudentAccessAsync(http, studentRepository, targetSession.IdStudent);
                if (targetAccessValidation is not null)
                {
                    return targetAccessValidation;
                }

                existingResult.IdSession = dto.IdSession;
                existingResult.IdLevel = dto.IdLevel;
                existingResult.FinishingTime = dto.FinishingTime;
                existingResult.Attempts = dto.Attempts;
                existingResult.Fails = dto.Fails;
                existingResult.Completed = dto.Completed;
                var receivedAtServerTime = DateTime.Now;

                await levelResultRepository.UpdateAsync(existingResult);
                await sessionRepository.UpdateEndDateAsync(targetSession.IdSession, receivedAtServerTime);
                if (existingSession.IdSession != targetSession.IdSession)
                {
                    await sessionRepository.ClearEndDateIfWithoutLevelResultsAsync(existingSession.IdSession);
                }
                return await EndpointResponseHelper.UpdateWithDetailsAsync(id, levelResultRepository.GetByIdWithRelationsAsync);
            });

            app.MapDelete("/level-results/{id:int}", async (int id, HttpContext http, ILevelResultRepository levelResultRepository, ISessionRepository sessionRepository, IStudentRepository studentRepository) =>
            {
                var existingResult = await levelResultRepository.GetByIdAsync(id);
                if (existingResult is null)
                {
                    return Results.NotFound();
                }

                var session = await sessionRepository.GetByIdAsync(existingResult.IdSession);
                if (session is null)
                {
                    return Results.NotFound();
                }

                var accessValidation = await ValidateStudentAccessAsync(http, studentRepository, session.IdStudent);
                if (accessValidation is not null)
                {
                    return accessValidation;
                }

                var deleted = await levelResultRepository.DeleteAsync(id);
                if (deleted)
                {
                    await sessionRepository.ClearEndDateIfWithoutLevelResultsAsync(session.IdSession);
                }

                return EndpointResponseHelper.DeleteResult(deleted);
            });
        }

        private static async Task<IResult?> ValidateStudentAccessAsync(HttpContext http, IStudentRepository studentRepository, int? studentId)
        {
            if (!studentId.HasValue)
            {
                return Results.Forbid();
            }

            var userId = http.User.GetUserId();
            var role = http.User.GetUserRole();

            if (!userId.HasValue)
            {
                return Results.Forbid();
            }

            if (role == "student")
            {
                return userId.Value == studentId.Value ? null : Results.Forbid();
            }

            if (role == "tutor")
            {
                return await studentRepository.IsStudentAssignedToTutorAsync(studentId.Value, userId.Value)
                    ? null
                    : Results.Forbid();
            }

            return Results.Forbid();
        }
    }
}
