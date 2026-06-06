using Microsoft.AspNetCore.Builder;
using MicroservicioFiguras.Interfaces;

namespace MicroservicioFiguras.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dashboard")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/summary", async (IDashboardRepository repository) =>
            Results.Ok(await repository.GetSummaryAsync()));
    }
}