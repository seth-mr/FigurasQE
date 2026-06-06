using System.Threading.Tasks;
using MicroservicioFiguras.DTOs;

namespace MicroservicioFiguras.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}