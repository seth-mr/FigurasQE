using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class SessionRepository : Repository<Session>, ISessionRepository
{
    private readonly FigurasqeContext _context;

    public SessionRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<SessionDto>> GetAllWithRelationsAsync()
    {
        return await _context.Sessions
            .Include(s => s.IdStudentNavigation)
            .Include(s => s.LevelResults)
            .AsNoTracking()
            .Select(s => new SessionDto
            {
                IdSession = s.IdSession,
                IdStudent = s.IdStudent,
                BeginningDate = s.BeginningDate,
                EndDate = s.EndDate,
                Device = s.Device,
                Student = s.IdStudentNavigation == null ? null : new StudentBasicDto
                {
                    IdStudent = s.IdStudentNavigation.IdStudent,
                    IdTutor = s.IdStudentNavigation.IdTutor,
                    Name = s.IdStudentNavigation.Name,
                    Age = s.IdStudentNavigation.Age,
                    Gender = s.IdStudentNavigation.Gender,
                    Country = s.IdStudentNavigation.Country
                },
                LevelResults = s.LevelResults.Select(r => new LevelResultBasicDto
                {
                    IdResult = r.IdResult,
                    IdLevel = r.IdLevel,
                    IdSession = r.IdSession,
                    Completed = r.Completed
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<SessionDto?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Sessions
            .Include(s => s.IdStudentNavigation)
            .Include(s => s.LevelResults)
            .AsNoTracking()
            .Where(s => s.IdSession == id)
            .Select(s => new SessionDto
            {
                IdSession = s.IdSession,
                IdStudent = s.IdStudent,
                BeginningDate = s.BeginningDate,
                EndDate = s.EndDate,
                Device = s.Device,
                Student = s.IdStudentNavigation == null ? null : new StudentBasicDto
                {
                    IdStudent = s.IdStudentNavigation.IdStudent,
                    IdTutor = s.IdStudentNavigation.IdTutor,
                    Name = s.IdStudentNavigation.Name,
                    Age = s.IdStudentNavigation.Age,
                    Gender = s.IdStudentNavigation.Gender,
                    Country = s.IdStudentNavigation.Country
                },
                LevelResults = s.LevelResults.Select(r => new LevelResultBasicDto
                {
                    IdResult = r.IdResult,
                    IdLevel = r.IdLevel,
                    IdSession = r.IdSession,
                    Completed = r.Completed
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateEndDateAsync(int sessionId, DateTime? endDate)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.IdSession == sessionId);
        if (session is null)
        {
            return false;
        }

        session.EndDate = NormalizeTimestampWithoutTimeZone(endDate);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearEndDateIfWithoutLevelResultsAsync(int sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.LevelResults)
            .FirstOrDefaultAsync(s => s.IdSession == sessionId);

        if (session is null)
        {
            return false;
        }

        if (session.LevelResults.Count != 0 || session.EndDate is null)
        {
            return true;
        }

        session.EndDate = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private static DateTime? NormalizeTimestampWithoutTimeZone(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var normalized = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value.ToLocalTime(),
            _ => value.Value
        };

        return DateTime.SpecifyKind(normalized, DateTimeKind.Unspecified);
    }
}
