using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Controllers;

public class LogsController
{
    private readonly AdminLogsApiClient _logsApiClient;
    private readonly AdminSessionState _sessionState;

    public LogsController(AdminLogsApiClient logsApiClient, AdminSessionState sessionState)
    {
        _logsApiClient = logsApiClient;
        _sessionState = sessionState;
    }

    public Task<IReadOnlyList<AdminLogEvent>> GetLogsAsync(
        string? service,
        string? logType,
        string? entityType,
        string? action,
        string? statusClass,
        string? route)
    {
        return _logsApiClient.GetLogsAsync(GetSession().Token, service, logType, entityType, action, statusClass, route);
    }

    public Task ListenAsync(
        string? service,
        string? logType,
        string? entityType,
        string? action,
        string? statusClass,
        string? route,
        Action<AdminLogEvent> onLogReceived,
        Action<bool>? onConnectionChanged,
        CancellationToken cancellationToken)
    {
        return _logsApiClient.ListenAsync(GetSession().Token, service, logType, entityType, action, statusClass, route, onLogReceived, onConnectionChanged, cancellationToken);
    }

    private AdminSession GetSession()
    {
        return _sessionState.GetSession();
    }
}