using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class TutorRepository : Repository<Tutor>, ITutorRepository
{
    private readonly FigurasqeContext _context;

    public TutorRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<TutorDto>> GetAllWithStudentsAsync()
    {
        return await _context.Tutors
            .Include(t => t.Students)
            .AsNoTracking()
            .Select(t => new TutorDto
            {
                IdTutor = t.IdTutor,
                Name = t.Name,
                Email = t.Email,
                Country = t.Country ?? string.Empty,
                Gender = t.Gender,
                Age = t.Age,
                Degree = t.Degree,
                RegistrationDate = t.RegistrationDate,
                Students = t.Students.Select(s => new StudentBasicDto
                {
                    IdStudent = s.IdStudent,
                    IdTutor = s.IdTutor,
                    Name = s.Name,
                    Age = s.Age,
                    Gender = s.Gender,
                    Country = s.Country
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<TutorDto?> GetByIdWithStudentsAsync(int id)
    {
        return await _context.Tutors
            .Include(t => t.Students)
            .AsNoTracking()
            .Where(t => t.IdTutor == id)
            .Select(t => new TutorDto
            {
                IdTutor = t.IdTutor,
                Name = t.Name,
                Email = t.Email,
                Country = t.Country ?? string.Empty,
                Gender = t.Gender,
                Age = t.Age,
                Degree = t.Degree,
                RegistrationDate = t.RegistrationDate,
                Students = t.Students.Select(s => new StudentBasicDto
                {
                    IdStudent = s.IdStudent,
                    IdTutor = s.IdTutor,
                    Name = s.Name,
                    Age = s.Age,
                    Gender = s.Gender,
                    Country = s.Country
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
