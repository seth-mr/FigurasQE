using System.Net.Http.Headers;
using System.Net.Http.Json;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages;

public class LevelCompleteModel : PageModel
{
    private const string SessionKey = "active_game_session_id";
    private readonly string SessionsRoute;
    private readonly string LevelResultsRoute;
    private readonly HttpClient client;

    public LevelCompleteModel(HttpClient http, IConfiguration configuration)
    {
        client = http;
        SessionsRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/sessions");
        LevelResultsRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/level-results");
    }

    [BindProperty(SupportsGet = true)]
    public string NextLevel { get; set; } = "/Levels/LevelsCatalog";

    [BindProperty(SupportsGet = true)]
    public int LevelId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int FinishingTime { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CompletionId { get; set; }

    public string? SaveMessage { get; set; }

    public bool ProgressSaved { get; set; }

    public async Task OnGetAsync()
    {
        NormalizeNextLevel();

        if (LevelId <= 0 || FinishingTime < 0)
            return;

        if (!User.IsInRole("student"))
        {
            SaveMessage = User.Identity?.IsAuthenticated == true
                ? "Este avance no se guardo porque solo las cuentas de alumno registran sesiones de juego."
                : "Inicia sesion como alumno para guardar tus sesiones.";
            return;
        }

        if (WasAlreadyRecorded())
        {
            ProgressSaved = true;
            SaveMessage = "Este nivel ya estaba guardado.";
            return;
        }

        var token = User.FindFirst("jwt_token")?.Value;
        var userIdValue = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(userIdValue, out var studentId))
        {
            SaveMessage = "No se pudo guardar el avance porque la sesion expiro.";
            return;
        }

        var sessionId = await EnsureGameSessionAsync(studentId, token);
        if (!sessionId.HasValue)
        {
            SaveMessage = "No se pudo crear la sesion de juego.";
            return;
        }

        ProgressSaved = await SaveLevelResultAsync(sessionId.Value, token);
        SaveMessage = ProgressSaved
            ? "Avance guardado en tu historial."
            : "Completaste el nivel, pero no se pudo guardar el avance.";

        if (ProgressSaved)
        {
            MarkAsRecorded();

            if (NextLevel.Equals("/Levels/LevelsCatalog", StringComparison.OrdinalIgnoreCase))
                HttpContext.Session.Remove(SessionKey);
        }
    }

    private async Task<int?> EnsureGameSessionAsync(int studentId, string token)
    {
        var existingSessionId = HttpContext.Session.GetInt32(SessionKey);
        if (existingSessionId.HasValue)
            return existingSessionId.Value;

        var request = CreateAuthorizedRequest(HttpMethod.Post, SessionsRoute, token);
        request.Content = JsonContent.Create(new CreateGameSessionRequest
        {
            IdStudent = studentId,
            BeginningDate = DateTime.SpecifyKind(
                DateTime.UtcNow.AddMilliseconds(-FinishingTime),
                DateTimeKind.Unspecified),
            Device = "web"
        });

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var session = await response.Content.ReadFromJsonAsync<SessionDto>();
        if (session?.IdSession is null or <= 0)
            return null;

        HttpContext.Session.SetInt32(SessionKey, session.IdSession);
        return session.IdSession;
    }

    private async Task<bool> SaveLevelResultAsync(int sessionId, string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Post, LevelResultsRoute, token);
        request.Content = JsonContent.Create(new CreateLevelResultRequest
        {
            IdSession = sessionId,
            IdLevel = LevelId,
            FinishingTime = FinishingTime,
            Attempts = 1,
            Fails = 0,
            Completed = true
        });

        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private bool WasAlreadyRecorded()
    {
        return !string.IsNullOrWhiteSpace(CompletionId)
            && HttpContext.Session.GetString(GetCompletionSessionKey()) == "true";
    }

    private void MarkAsRecorded()
    {
        if (!string.IsNullOrWhiteSpace(CompletionId))
            HttpContext.Session.SetString(GetCompletionSessionKey(), "true");
    }

    private string GetCompletionSessionKey()
    {
        return $"level_completion_{CompletionId}";
    }

    private void NormalizeNextLevel()
    {
        if (string.IsNullOrWhiteSpace(NextLevel) || !NextLevel.StartsWith("/Levels/", StringComparison.OrdinalIgnoreCase))
            NextLevel = "/Levels/LevelsCatalog";
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
