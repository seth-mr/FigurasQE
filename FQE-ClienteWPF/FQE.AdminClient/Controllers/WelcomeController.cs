using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Controllers;

public class WelcomeController
{
    private readonly AdminSessionState _sessionState;

    public WelcomeController(AdminSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public AdminProfile GetCurrentAdmin()
    {
        return _sessionState.CurrentAdmin ?? throw new InvalidOperationException("No hay una sesion de admin activa.");
    }

    public string GetWelcomeTitle()
    {
        var admin = GetCurrentAdmin();
        return $"Bienvenido, {admin.Name}";
    }

    public string GetWelcomeSubtitle()
    {
        var admin = GetCurrentAdmin();
        return $"Has iniciado sesion como {admin.Username}. Desde aqui podras entrar a tu cuenta y mas adelante a otras operaciones de administracion.";
    }
}