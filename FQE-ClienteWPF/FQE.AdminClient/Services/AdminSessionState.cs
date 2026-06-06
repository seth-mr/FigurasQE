using FQE.AdminClient.Models;

namespace FQE.AdminClient.Services;

public class AdminSessionState
{
    private AdminSession? _session;

    public bool IsAuthenticated => _session is not null;

    public AdminProfile? CurrentAdmin => _session?.Profile;

    public void Start(AdminLoginResponse response)
    {
        if (response.Admin is null || string.IsNullOrWhiteSpace(response.Token))
        {
            throw new InvalidOperationException("La respuesta de autenticacion no contiene una sesion valida.");
        }

        _session = new AdminSession
        {
            IdAdmin = response.Admin.IdAdmin,
            Token = response.Token,
            Profile = response.Admin
        };
    }

    public AdminSession GetSession()
    {
        return _session ?? throw new InvalidOperationException("No hay una sesion de admin activa.");
    }

    public void UpdateProfile(AdminProfile profile)
    {
        if (_session is null)
        {
            throw new InvalidOperationException("No hay una sesion de admin activa.");
        }

        _session.Profile = profile;
        _session.IdAdmin = profile.IdAdmin;
    }

    public void Clear()
    {
        _session = null;
    }
}