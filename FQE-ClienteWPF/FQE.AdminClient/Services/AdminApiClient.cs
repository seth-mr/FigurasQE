using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FQE.AdminClient.Models;

namespace FQE.AdminClient.Services;

public class AdminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminApiClient(string gatewayBaseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/")
        };
    }

    public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("auth/admin/login", request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, "No se pudo iniciar sesion como admin.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AdminLoginResponse>(_jsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Token) || payload.Admin is null || payload.Role != "admin")
        {
            throw new InvalidOperationException("La respuesta del servidor no contiene un admin valido.");
        }

        return payload;
    }

    public async Task<AdminProfile> GetAdminAsync(int adminId, string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"data/admins/{adminId}", token);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, "No se pudieron cargar los datos del admin.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AdminProfile>(_jsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException("La respuesta del servidor no incluye datos del admin.");
        }

        return payload;
    }

    public async Task<AdminProfile> UpdateAdminAsync(int adminId, string token, UpdateAdminRequest requestBody)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"data/admins/{adminId}", token);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, "No se pudieron actualizar los datos del admin.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AdminProfile>(_jsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException("El servidor no devolvio el perfil actualizado.");
        }

        return payload;
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "data/dashboard/summary", token);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, "No se pudo cargar el resumen estadistico.");
        }

        var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>(_jsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException("El servidor no devolvio un resumen estadistico valido.");
        }

        return payload;
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
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
                if (document.RootElement.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
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