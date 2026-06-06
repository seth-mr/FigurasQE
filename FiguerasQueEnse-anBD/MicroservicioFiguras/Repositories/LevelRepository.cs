using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class LevelRepository : Repository<Level>, ILevelRepository
{
    private readonly FigurasqeContext _context;

    public LevelRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<LevelDto>> GetAllWithResultsAsync()
    {
        return await _context.Levels
            .Include(l => l.LevelResults)
            .AsNoTracking()
            .Select(l => new LevelDto
            {
                IdLevel = l.IdLevel,
                Name = l.Name,
                Difficulty = l.Difficulty,
                LevelResults = l.LevelResults.Select(r => new LevelResultBasicDto
                {
                    IdResult = r.IdResult,
                    IdLevel = r.IdLevel,
                    IdSession = r.IdSession,
                    Completed = r.Completed
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<LevelDto?> GetByIdWithResultsAsync(int id)
    {
        return await _context.Levels
            .Include(l => l.LevelResults)
            .AsNoTracking()
            .Where(l => l.IdLevel == id)
            .Select(l => new LevelDto
            {
                IdLevel = l.IdLevel,
                Name = l.Name,
                Difficulty = l.Difficulty,
                LevelResults = l.LevelResults.Select(r => new LevelResultBasicDto
                {
                    IdResult = r.IdResult,
                    IdLevel = r.IdLevel,
                    IdSession = r.IdSession,
                    Completed = r.Completed
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
