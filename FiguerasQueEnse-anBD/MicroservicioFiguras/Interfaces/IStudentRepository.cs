using System.Collections.Generic;
using System.Threading.Tasks;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<List<StudentDto>> GetAllWithTutorAsync();
    Task<StudentDto?> GetByIdWithTutorAsync(int id);
    Task<List<int>> GetStudentIdsByTutorAsync(int tutorId);
    Task<List<StudentDto>> GetStudentsByTutorIdAsync(int tutorId);
    Task<bool> IsStudentAssignedToTutorAsync(int studentId, int tutorId);
    Task<List<int>> GetSessionIdsByStudentAsync(int studentId);
    Task<bool> IsEmailTakenAsync(string email);
    Task<bool> IsEmailTakenByOtherAsync(int id, string email);
    Task<bool> TutorExistsAsync(int tutorId);
    Task<AssignTutorByEmailResult> AssignTutorByEmailAsync(string studentEmail, string tutorEmail);
}
