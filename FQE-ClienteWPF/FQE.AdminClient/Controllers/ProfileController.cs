using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Controllers;

public class ProfileController
{
    private readonly AdminApiClient _apiClient;
    private readonly AdminSessionState _sessionState;

    public ProfileController(AdminApiClient apiClient, AdminSessionState sessionState)
    {
        _apiClient = apiClient;
        _sessionState = sessionState;
    }

    public AdminProfile GetCachedProfile()
    {
        return _sessionState.CurrentAdmin ?? throw new InvalidOperationException("No hay una sesion de admin activa.");
    }

    public async Task<AdminProfile> RefreshAsync()
    {
        var session = RequireSession();
        var profile = await _apiClient.GetAdminAsync(session.IdAdmin, session.Token);
        _sessionState.UpdateProfile(profile);
        return profile;
    }

    public async Task<AdminProfile> SaveAsync(UpdateAdminRequest request)
    {
        var session = RequireSession();
        var profile = await _apiClient.UpdateAdminAsync(session.IdAdmin, session.Token, request);
        _sessionState.UpdateProfile(profile);
        return profile;
    }

    private AdminSession RequireSession()
    {
        return _sessionState.GetSession();
    }
}