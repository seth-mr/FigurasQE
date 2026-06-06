using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class AdminRepository : Repository<Admin>, IAdminRepository
{
    private readonly FigurasqeContext _context;

    public AdminRepository(FigurasqeContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<AdminDto>> GetAllDetailsAsync()
    {
        return await _context.Admins
            .AsNoTracking()
            .Select(admin => MapToDto(admin))
            .ToListAsync();
    }

    public Task<bool> HasAnyAdminsAsync()
    {
        return _context.Admins.AsNoTracking().AnyAsync();
    }

    public async Task<AdminDto?> GetByIdDetailsAsync(int id)
    {
        return await _context.Admins
            .AsNoTracking()
            .Where(admin => admin.IdAdmin == id)
            .Select(admin => MapToDto(admin))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludedAdminId = null)
    {
        var adminEmailInUse = await _context.Admins
            .AsNoTracking()
            .AnyAsync(admin => admin.Email == email && (!excludedAdminId.HasValue || admin.IdAdmin != excludedAdminId.Value));

        if (adminEmailInUse)
        {
            return true;
        }

        var studentEmailInUse = await _context.Students
            .AsNoTracking()
            .AnyAsync(student => student.Email == email);

        if (studentEmailInUse)
        {
            return true;
        }

        return await _context.Tutors
            .AsNoTracking()
            .AnyAsync(tutor => tutor.Email == email);
    }

    public Task<bool> UsernameExistsAsync(string username, int? excludedAdminId = null)
    {
        return _context.Admins
            .AsNoTracking()
            .AnyAsync(admin => admin.Username == username && (!excludedAdminId.HasValue || admin.IdAdmin != excludedAdminId.Value));
    }

    private static AdminDto MapToDto(Admin admin)
    {
        return new AdminDto
        {
            IdAdmin = admin.IdAdmin,
            Name = admin.Name,
            Email = admin.Email,
            Phone = admin.Phone,
            Username = admin.Username,
            RegistrationDate = admin.RegistrationDate
        };
    }
}