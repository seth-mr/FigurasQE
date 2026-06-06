using System.Collections.Generic;
using System.Threading.Tasks;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Interfaces;

public interface ILevelResultRepository : IRepository<LevelResult>
{
    Task<List<LevelResultDto>> GetAllWithRelationsAsync();
    Task<LevelResultDto?> GetByIdWithRelationsAsync(int id);
    Task<List<int>> GetIdsBySessionAsync(int sessionId);
}
