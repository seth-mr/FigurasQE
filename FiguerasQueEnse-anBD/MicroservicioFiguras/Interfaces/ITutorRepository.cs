using System.Collections.Generic;
using System.Threading.Tasks;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Interfaces;

public interface ITutorRepository : IRepository<Tutor>
{
    Task<List<TutorDto>> GetAllWithStudentsAsync();
    Task<TutorDto?> GetByIdWithStudentsAsync(int id);
}
