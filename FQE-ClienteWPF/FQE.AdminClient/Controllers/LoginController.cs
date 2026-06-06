using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Controllers;

public class LoginController
{
    private readonly AdminApiClient _apiClient;
    private readonly AdminSessionState _sessionState;

    public LoginController(AdminApiClient apiClient, AdminSessionState sessionState)
    {
        _apiClient = apiClient;
        _sessionState = sessionState;
    }

    public async Task<AdminProfile> LoginAsync(string email, string password)
    {
        var response = await _apiClient.LoginAsync(new AdminLoginRequest
        {
            Email = email,
            Password = password
        });

        _sessionState.Start(response);
        return response.Admin!;
    }

    public void Logout()
    {
        _sessionState.Clear();
    }
}