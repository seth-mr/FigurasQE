using System.Collections.Generic;
using System.Threading.Tasks;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Interfaces;

public interface ILevelRepository : IRepository<Level>
{
    Task<List<LevelDto>> GetAllWithResultsAsync();
    Task<LevelDto?> GetByIdWithResultsAsync(int id);
}
