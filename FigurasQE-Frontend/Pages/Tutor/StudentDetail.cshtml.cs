using System.Net.Http.Headers;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages.Tutor;

[Authorize(Roles = "tutor")]
public class StudentDetailModel : PageModel
{
    private readonly HttpClient client;
    private readonly string StudentRoute;

    public StudentDetailModel(HttpClient http, IConfiguration configuration)
    {
        client = http;
        StudentRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/students/");
    }

    public StudentDto Student { get; set; } = new();

    public List<SessionDto> Sessions { get; set; } = [];

    public List<SessionDto> FilteredSessions { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "datos";

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = User.FindFirst("jwt_token")?.Value;

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("/User/Login");

        var studentRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{StudentRoute}{id}", token);
        var sessionsRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{StudentRoute}{id}/sessions", token);

        var studentResponse = await client.SendAsync(studentRequest);
        var sessionsResponse = await client.SendAsync(sessionsRequest);

        if (!studentResponse.IsSuccessStatusCode)
        {
            ErrorMessage = "No se pudo cargar el alumno.";
            return Page();
        }

        Student = await studentResponse.Content.ReadFromJsonAsync<StudentDto>() ?? new StudentDto();

        if (sessionsResponse.IsSuccessStatusCode)
        {
            Sessions = SessionDateHelper.NormalizeWebSessionDates(
                (await sessionsResponse.Content.ReadFromJsonAsync<List<SessionDto>>() ?? []))
                .OrderByDescending(session => session.BeginningDate)
                .ToList();
        }
        else
        {
            ErrorMessage = "Se cargaron los datos del alumno, pero no se pudo cargar su historial de sesiones.";
        }

        FilteredSessions = FilterSessions(Sessions, Filter).ToList();
        Tab = Tab.Equals("sesiones", StringComparison.OrdinalIgnoreCase) ? "sesiones" : "datos";

        return Page();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static IEnumerable<SessionDto> FilterSessions(IEnumerable<SessionDto> sessions, string filter)
    {
        if (filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return sessions;

        var now = SessionDateHelper.MexicoNow();
        var todayStart = now.Date;
        var lowerBound = filter.ToLowerInvariant() switch
        {
            "today" => todayStart,
            "5d" => todayStart.AddDays(-4),
            "7d" => todayStart.AddDays(-6),
            "15d" => todayStart.AddDays(-14),
            "1m" => todayStart.AddMonths(-1),
            "2m" => todayStart.AddMonths(-2),
            _ => DateTime.MinValue
        };

        return sessions.Where(session =>
            session.BeginningDate.HasValue
            && session.BeginningDate.Value >= lowerBound
            && session.BeginningDate.Value <= now);
    }

    public string FormatGender(char gender)
    {
        return char.ToUpperInvariant(gender) switch
        {
            'M' => "Masculino",
            'F' => "Femenino",
            'O' => "Otro",
            _ => "No disponible"
        };
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

    public string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dd MMM yyyy, HH:mm") : "No disponible";
    }

    public string FormatEndDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dd MMM yyyy, HH:mm") : "En curso";
    }

    public string FormatDuration(SessionDto session)
    {
        if (!session.BeginningDate.HasValue)
            return "Duración no disponible";

        if (!session.EndDate.HasValue)
            return "En curso";

        var totalMinutes = Math.Max(0, (long)(session.EndDate.Value - session.BeginningDate.Value).TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes} min";
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

    public int PlayedLevelsCount()
    {
        return Sessions.Sum(session => session.LevelResults.Count);
    }

    public int CompletionPercentage()
    {
        var played = PlayedLevelsCount();
        return played == 0 ? 0 : CompletedLevelsCount() * 100 / played;
    }

    public bool IsNeurodivergent()
    {
        return !string.IsNullOrWhiteSpace(Student.Neurodivergency)
            && !Student.Neurodivergency.Equals("NULL", StringComparison.OrdinalIgnoreCase)
            && !Student.Neurodivergency.Equals("ninguna", StringComparison.OrdinalIgnoreCase);
    }

    public string NeurodivergencyText()
    {
        return IsNeurodivergent() ? Student.Neurodivergency! : "No reportada";
    }

    public string GetStudentCardClass()
    {
        return char.ToUpperInvariant(Student.Gender) switch
        {
            'M' => "student-detail-male",
            'F' => "student-detail-female",
            _ => "student-detail-neutral"
        };
    }

    public IReadOnlyList<(string Value, string Label)> SessionFilters { get; } =
    [
        ("all", "Todas"),
        ("today", "Hoy"),
        ("5d", "5 días"),
        ("7d", "7 días"),
        ("15d", "15 días"),
        ("1m", "1 mes"),
        ("2m", "2 meses")
    ];

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.HttpContext.Response.Headers["Pragma"] = "no-cache";
        context.HttpContext.Response.Headers["Expires"] = "0";
    }
}
