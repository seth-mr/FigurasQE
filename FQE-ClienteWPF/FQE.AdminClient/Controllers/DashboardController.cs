using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Controllers;

public class DashboardController
{
    private readonly AdminApiClient _apiClient;
    private readonly AdminSessionState _sessionState;

    public DashboardController(AdminApiClient apiClient, AdminSessionState sessionState)
    {
        _apiClient = apiClient;
        _sessionState = sessionState;
    }

    public Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var token = _sessionState.GetSession().Token;
        return _apiClient.GetDashboardSummaryAsync(token);
    }
}
