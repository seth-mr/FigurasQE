using System.Net.Http.Headers;
using System.Text.Json;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages.Tutor;

[Authorize(Roles = "tutor")]
public class HomeModel : PageModel
{
    private readonly HttpClient client;
    private readonly string TutorRoute;
    private readonly string AssignStudentRoute;

    public HomeModel(HttpClient http, IConfiguration configuration)
    {
        client = http;
        TutorRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/tutors/");
        AssignStudentRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/tutors/assign-student");
    }

    public TutorDto Tutor { get; set; } = new();

    public List<StudentDto> Students { get; set; } = [];

    [BindProperty]
    public string StudentEmail { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? SuccessMessage { get; set; }

    public string? AssignmentFeedbackMessage { get; set; }

    public bool HasAssignmentFeedback => !string.IsNullOrWhiteSpace(AssignmentFeedbackMessage);

    public bool IsAssignmentFeedbackError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostAssignStudentAsync()
    {
        var token = User.FindFirst("jwt_token")?.Value;

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("/User/Login");

        if (string.IsNullOrWhiteSpace(StudentEmail))
        {
            AssignmentFeedbackMessage = "Escribe el correo del alumno que quieres asignar.";
            IsAssignmentFeedbackError = true;
            await LoadTutorDataAsync(token);
            return Page();
        }

        var request = new HttpRequestMessage(HttpMethod.Post, AssignStudentRoute)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new AssignStudentRequest
                {
                    StudentEmail = StudentEmail.Trim(),
                    TutorEmail = User.Identity?.Name ?? string.Empty
                }),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            AssignmentFeedbackMessage = await ResolveErrorMessageAsync(response, "No se pudo asignar el alumno.");
            IsAssignmentFeedbackError = true;
            await LoadTutorDataAsync(token);
            return Page();
        }

        AssignmentFeedbackMessage = $"Alumno {StudentEmail.Trim()} asignado correctamente. Ya aparece en tu lista.";
        IsAssignmentFeedbackError = false;
        SuccessMessage = AssignmentFeedbackMessage;
        StudentEmail = string.Empty;

        await LoadTutorDataAsync(token);
        return Page();
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        var token = User.FindFirst("jwt_token")?.Value;

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("/User/Login");

        var loaded = await LoadTutorDataAsync(token);
        return loaded ? Page() : RedirectToPage("/User/Login");
    }

    private async Task<bool> LoadTutorDataAsync(string token)
    {
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var tutorRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{TutorRoute}{userId}", token);
        var tutorResponse = await client.SendAsync(tutorRequest);

        if (!tutorResponse.IsSuccessStatusCode)
            return false;

        Tutor = await tutorResponse.Content.ReadFromJsonAsync<TutorDto>() ?? new TutorDto();
        HttpContext.Session.SetString("tutor", JsonSerializer.Serialize(Tutor));

        var studentsRequest = CreateAuthorizedRequest(HttpMethod.Get, $"{TutorRoute}{userId}/students", token);
        var studentsResponse = await client.SendAsync(studentsRequest);

        if (studentsResponse.IsSuccessStatusCode)
        {
            Students = (await studentsResponse.Content.ReadFromJsonAsync<List<StudentDto>>() ?? [])
                .OrderBy(student => student.Name)
                .ToList();
        }
        else
        {
            Students = [];
            ErrorMessage ??= "No se pudo cargar la lista de alumnos.";
        }

        return true;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            var gatewayError = JsonSerializer.Deserialize<GatewayErrorResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (gatewayError?.Errors?.Count > 0)
                return string.Join(" ", gatewayError.Errors.SelectMany(error => error.Value));

            if (!string.IsNullOrWhiteSpace(gatewayError?.Message))
                return gatewayError.Message;
        }
        catch (JsonException)
        {
            return body;
        }

        return body;
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

    public string FormatGender(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "M" => "Masculino",
            "F" => "Femenino",
            "O" => "Otro",
            _ => string.IsNullOrWhiteSpace(value) ? "No disponible" : value
        };
    }

    public string FormatAge(int? age)
    {
        var value = age.GetValueOrDefault();
        return value > 0 ? value.ToString() : "No disponible";
    }

    public string FormatDegree(string? value)
    {
        return value switch
        {
            "licenciatura" => "Licenciatura",
            "maestria" => "Maestría",
            "doctorado" => "Doctorado",
            "postdoctorado" => "Post Doctorado",
            "padre-madre" => "Padre o Madre",
            "otro" => "Otro",
            _ => string.IsNullOrWhiteSpace(value) ? "No disponible" : value
        };
    }

    public bool IsNeurodivergent(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !value.Equals("NULL", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("ninguna", StringComparison.OrdinalIgnoreCase);
    }

    public string GetGenderCardClass(char gender)
    {
        return char.ToUpperInvariant(gender) switch
        {
            'M' => "student-card-male",
            'F' => "student-card-female",
            _ => "student-card-neutral"
        };
    }

    public string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "A";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.HttpContext.Response.Headers["Pragma"] = "no-cache";
        context.HttpContext.Response.Headers["Expires"] = "0";
    }
}
