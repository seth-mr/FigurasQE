using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Interfaces;

public interface IAdminRepository : IRepository<Admin>
{
    Task<List<AdminDto>> GetAllDetailsAsync();
    Task<AdminDto?> GetByIdDetailsAsync(int id);
    Task<bool> HasAnyAdminsAsync();
    Task<bool> EmailExistsAsync(string email, int? excludedAdminId = null);
    Task<bool> UsernameExistsAsync(string username, int? excludedAdminId = null);
}