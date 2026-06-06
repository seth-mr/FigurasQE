using System.Net.Http.Headers;
using System.Text.Json;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages.Student;

[Authorize(Roles = "student")]
public class HomeModel : PageModel
{
    private readonly HttpClient client;
    private readonly string StudentRoute;

    public HomeModel(HttpClient http, IConfiguration configuration)
    {
        client = http;
        StudentRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/students/");
    }

    public StudentDto Student { get; set; } = new();

    public List<SessionDto> Sessions { get; set; } = [];

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = User.FindFirst("jwt_token")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/User/Login");

        var studentRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{StudentRoute}{userId}", token);
        var sessionsRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{StudentRoute}{userId}/sessions", token);

        var studentResponse = await client.SendAsync(studentRequest);
        var sessionsResponse = await client.SendAsync(sessionsRequest);

        if (!studentResponse.IsSuccessStatusCode)
            return RedirectToPage("/User/Login");

        Student = await studentResponse.Content.ReadFromJsonAsync<StudentDto>() ?? new StudentDto();
        HttpContext.Session.SetString("student", JsonSerializer.Serialize(Student));

        if (sessionsResponse.IsSuccessStatusCode)
        {
            Sessions = SessionDateHelper.NormalizeWebSessionDates(
                (await sessionsResponse.Content.ReadFromJsonAsync<List<SessionDto>>() ?? []))
                .OrderByDescending(session => session.BeginningDate)
                .ToList();
        }
        else
        {
            ErrorMessage = "Se cargó tu perfil, pero no se pudo cargar tu resumen de sesiones.";
        }

        return Page();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public string FormatCountry(string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "MX" => "México",
            "US" => "Estados Unidos",
            "ES" => "España",
            _ => string.IsNullOrWhiteSpace(value) ? "No disponible" : value
        };
    }

    public string FormatTotalPlayedTime()
    {
        var totalMinutes = Sessions.Sum(session =>
            session.BeginningDate.HasValue && session.EndDate.HasValue
                ? Math.Max(0, (long)(session.EndDate.Value - session.BeginningDate.Value).TotalMinutes)
                : 0);

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return totalMinutes switch
        {
            0 => "0 h jugadas",
            _ when hours > 0 && minutes > 0 => $"{hours}h {minutes}m jugadas",
            _ when hours > 0 => $"{hours} h jugadas",
            _ => $"{minutes} min jugados"
        };
    }

    public int CompletedLevelsCount()
    {
        return Sessions.Sum(session => session.LevelResults.Count(result => result.Completed == true));
    }

    public string LastSessionText()
    {
        return Sessions.FirstOrDefault()?.BeginningDate?.ToString("dd MMM yyyy, HH:mm") ?? "Sin sesiones";
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.HttpContext.Response.Headers["Pragma"] = "no-cache";
        context.HttpContext.Response.Headers["Expires"] = "0";
    }
}
