using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class LevelResultRepository : Repository<LevelResult>, ILevelResultRepository
{
    private readonly FigurasqeContext _context;

    public LevelResultRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<LevelResultDto>> GetAllWithRelationsAsync()
    {
        return await _context.LevelResults
            .Include(r => r.IdLevelNavigation)
            .Include(r => r.IdSessionNavigation)
            .AsNoTracking()
            .Select(r => new LevelResultDto
            {
                IdResult = r.IdResult,
                IdSession = r.IdSession,
                IdLevel = r.IdLevel,
                FinishingTime = r.FinishingTime,
                Attempts = r.Attempts,
                Fails = r.Fails,
                Completed = r.Completed,
                Level = r.IdLevelNavigation == null ? null : new LevelBasicDto
                {
                    IdLevel = r.IdLevelNavigation.IdLevel,
                    Name = r.IdLevelNavigation.Name,
                    Difficulty = r.IdLevelNavigation.Difficulty
                },
                Session = r.IdSessionNavigation == null ? null : new SessionBasicDto
                {
                    IdSession = r.IdSessionNavigation.IdSession,
                    IdStudent = r.IdSessionNavigation.IdStudent,
                    Device = r.IdSessionNavigation.Device
                }
            })
            .ToListAsync();
    }

    public async Task<LevelResultDto?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.LevelResults
            .Include(r => r.IdLevelNavigation)
            .Include(r => r.IdSessionNavigation)
            .AsNoTracking()
            .Where(r => r.IdResult == id)
            .Select(r => new LevelResultDto
            {
                IdResult = r.IdResult,
                IdSession = r.IdSession,
                IdLevel = r.IdLevel,
                FinishingTime = r.FinishingTime,
                Attempts = r.Attempts,
                Fails = r.Fails,
                Completed = r.Completed,
                Level = r.IdLevelNavigation == null ? null : new LevelBasicDto
                {
                    IdLevel = r.IdLevelNavigation.IdLevel,
                    Name = r.IdLevelNavigation.Name,
                    Difficulty = r.IdLevelNavigation.Difficulty
                },
                Session = r.IdSessionNavigation == null ? null : new SessionBasicDto
                {
                    IdSession = r.IdSessionNavigation.IdSession,
                    IdStudent = r.IdSessionNavigation.IdStudent,
                    Device = r.IdSessionNavigation.Device
                }
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<int>> GetIdsBySessionAsync(int sessionId)
    {
        return await _context.LevelResults
            .AsNoTracking()
            .Where(r => r.IdSession == sessionId)
            .Select(r => r.IdResult)
            .ToListAsync();
    }
}
