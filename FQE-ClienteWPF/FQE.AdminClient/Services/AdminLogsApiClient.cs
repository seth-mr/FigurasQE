using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FQE.AdminClient.Models;

namespace FQE.AdminClient.Services;

public class AdminLogsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminLogsApiClient(string logsBaseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(logsBaseUrl.TrimEnd('/') + "/")
        };
    }

    public async Task<IReadOnlyList<AdminLogEvent>> GetLogsAsync(
        string token,
        string? service,
        string? logType,
        string? entityType,
        string? action,
        string? statusClass,
        string? route)
    {
        var path = string.IsNullOrWhiteSpace(service)
            ? $"api/logs{BuildFilterQueryString(null, logType, entityType, action, statusClass, route)}"
            : $"api/logs/service/{Uri.EscapeDataString(service)}{BuildFilterQueryString(null, logType, entityType, action, statusClass, route)}";

        using var request = CreateAuthorizedRequest(HttpMethod.Get, path, token);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, "No se pudieron cargar los logs.");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<AdminLogEvent>>(_jsonOptions);
        return payload ?? [];
    }

    public async Task ListenAsync(
        string token,
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
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(BuildSocketUri(token, service, logType, entityType, action, statusClass, route), cancellationToken);
        onConnectionChanged?.Invoke(true);

        var buffer = new byte[16 * 1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }

                var json = Encoding.UTF8.GetString(stream.ToArray());
                var payload = JsonSerializer.Deserialize<AdminLogEvent>(json, _jsonOptions);
                if (payload is not null)
                {
                    onLogReceived(payload);
                }
            }
        }
        finally
        {
            onConnectionChanged?.Invoke(false);

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
            }
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private Uri BuildSocketUri(
        string token,
        string? service,
        string? logType,
        string? entityType,
        string? action,
        string? statusClass,
        string? route)
    {
        var baseAddress = _httpClient.BaseAddress ?? throw new InvalidOperationException("La URL del servicio de logs no esta configurada.");
        var builder = new UriBuilder(baseAddress)
        {
            Scheme = baseAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws",
            Path = "ws/logs"
        };

        var queryParts = new List<string>
        {
            $"token={Uri.EscapeDataString(token)}"
        };

        if (!string.IsNullOrWhiteSpace(service))
        {
            queryParts.Add($"service={Uri.EscapeDataString(service)}");
        }

        var extraFilters = BuildFilterQueryString(null, logType, entityType, action, statusClass, route).TrimStart('?');
        if (!string.IsNullOrWhiteSpace(extraFilters))
        {
            queryParts.Add(extraFilters);
        }

        builder.Query = string.Join("&", queryParts);
        return builder.Uri;
    }

    private static string BuildFilterQueryString(
        string? service,
        string? logType,
        string? entityType,
        string? action,
        string? statusClass,
        string? route)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(service))
        {
            parts.Add($"service={Uri.EscapeDataString(service)}");
        }

        if (!string.IsNullOrWhiteSpace(logType))
        {
            parts.Add($"type={Uri.EscapeDataString(logType)}");
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            parts.Add($"entityType={Uri.EscapeDataString(entityType)}");
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            parts.Add($"action={Uri.EscapeDataString(action)}");
        }

        if (!string.IsNullOrWhiteSpace(statusClass))
        {
            parts.Add($"statusClass={Uri.EscapeDataString(statusClass)}");
        }

        if (!string.IsNullOrWhiteSpace(route))
        {
            parts.Add($"route={Uri.EscapeDataString(route)}");
        }

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }

    private async Task<Exception> CreateApiExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var message = fallbackMessage;

        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("detail", out var detailElement) && detailElement.ValueKind == JsonValueKind.String)
                {
                    message = detailElement.GetString() ?? fallbackMessage;
                }
                else if (document.RootElement.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                {
                    message = messageElement.GetString() ?? fallbackMessage;
                }
                else if (document.RootElement.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                {
                    message = errorElement.GetString() ?? fallbackMessage;
                }
            }
        }
        catch (JsonException)
        {
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),
            HttpStatusCode.Forbidden => new UnauthorizedAccessException(message),
            _ => new InvalidOperationException(message)
        };
    }
}