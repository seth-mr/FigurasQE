using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class StudentRepository : Repository<Student>, IStudentRepository
{
    private readonly FigurasqeContext _context;

    public StudentRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<StudentDto>> GetAllWithTutorAsync()
    {
        return await _context.Students
            .Include(s => s.IdTutorNavigation)
            .AsNoTracking()
            .Select(s => new StudentDto
            {
                IdStudent = s.IdStudent,
                IdTutor = s.IdTutor,
                Name = s.Name,
                Email = s.Email,
                Age = s.Age,
                Gender = s.Gender,
                Country = s.Country,
                Neurodivergency = s.Neurodivergency,
                RegistrationDate = s.RegistrationDate,
                Tutor = s.IdTutorNavigation == null
                    ? null
                    : new TutorDto
                    {
                        IdTutor = s.IdTutorNavigation.IdTutor,
                        Name = s.IdTutorNavigation.Name,
                        Email = s.IdTutorNavigation.Email,
                        Country = s.IdTutorNavigation.Country ?? string.Empty
                    }
            })
            .ToListAsync();
    }

    public async Task<StudentDto?> GetByIdWithTutorAsync(int id)
    {
        var student = await _context.Students
            .Include(s => s.IdTutorNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdStudent == id);

        return student == null ? null : MapStudentDto(student);
    }

    public async Task<List<int>> GetStudentIdsByTutorAsync(int tutorId)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.IdTutor == tutorId)
            .Select(s => s.IdStudent)
            .ToListAsync();
    }

    public async Task<List<StudentDto>> GetStudentsByTutorIdAsync(int tutorId)
    {
        return await _context.Students
            .Include(s => s.IdTutorNavigation)
            .AsNoTracking()
            .Where(s => s.IdTutor == tutorId)
            .Select(s => new StudentDto
            {
                IdStudent = s.IdStudent,
                IdTutor = s.IdTutor,
                Name = s.Name,
                Email = s.Email,
                Age = s.Age,
                Gender = s.Gender,
                Country = s.Country,
                Neurodivergency = s.Neurodivergency,
                RegistrationDate = s.RegistrationDate,
                Tutor = s.IdTutorNavigation == null
                    ? null
                    : new TutorDto
                    {
                        IdTutor = s.IdTutorNavigation.IdTutor,
                        Name = s.IdTutorNavigation.Name,
                        Email = s.IdTutorNavigation.Email,
                        Country = s.IdTutorNavigation.Country ?? string.Empty
                    }
            })
            .ToListAsync();
    }

    public async Task<bool> IsStudentAssignedToTutorAsync(int studentId, int tutorId)
    {
        return await _context.Students
            .AsNoTracking()
            .AnyAsync(s => s.IdStudent == studentId && s.IdTutor == tutorId);
    }

    public async Task<List<int>> GetSessionIdsByStudentAsync(int studentId)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.IdStudent == studentId)
            .Select(s => s.IdSession)
            .ToListAsync();
    }

    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await _context.Students
            .AsNoTracking()
            .AnyAsync(s => s.Email == email);
    }

    public async Task<bool> IsEmailTakenByOtherAsync(int id, string email)
    {
        return await _context.Students
            .AsNoTracking()
            .AnyAsync(s => s.Email == email && s.IdStudent != id);
    }

    public async Task<bool> TutorExistsAsync(int tutorId)
    {
        return await _context.Tutors
            .AsNoTracking()
            .AnyAsync(t => t.IdTutor == tutorId);
    }

    internal static StudentDto MapStudentDto(Student student)
    {
        return new StudentDto
        {
            IdStudent = student.IdStudent,
            IdTutor = student.IdTutor,
            Name = student.Name,
            Email = student.Email,
            Age = student.Age,
            Gender = student.Gender,
            Country = student.Country,
            Neurodivergency = student.Neurodivergency,
            RegistrationDate = student.RegistrationDate,
            Tutor = student.IdTutorNavigation == null
                ? null
                : new TutorDto
                {
                    IdTutor = student.IdTutorNavigation.IdTutor,
                    Name = student.IdTutorNavigation.Name,
                    Email = student.IdTutorNavigation.Email,
                    Country = student.IdTutorNavigation.Country ?? string.Empty
                }
        };
    }

    public async Task<AssignTutorByEmailResult> AssignTutorByEmailAsync(string studentEmail, string tutorEmail)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
        if (student == null)
        {
            var tutorWithStudentEmail = await _context.Tutors
                .AsNoTracking()
                .AnyAsync(t => t.Email == studentEmail);

            return tutorWithStudentEmail
                ? AssignTutorByEmailResult.StudentEmailBelongsToTutor
                : AssignTutorByEmailResult.StudentNotFound;
        }

        var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.Email == tutorEmail);
        if (tutor == null)
        {
            return AssignTutorByEmailResult.TutorNotFound;
        }

        if (student.IdTutor == tutor.IdTutor)
        {
            return AssignTutorByEmailResult.StudentAlreadyAssignedToCurrentTutor;
        }

        if (student.IdTutor.HasValue && student.IdTutor.Value != tutor.IdTutor)
        {
            return AssignTutorByEmailResult.StudentAlreadyAssignedToAnotherTutor;
        }

        student.IdTutor = tutor.IdTutor;
        await _context.SaveChangesAsync();
        return AssignTutorByEmailResult.Success;
    }
}
