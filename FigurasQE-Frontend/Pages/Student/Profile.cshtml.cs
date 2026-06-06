using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FigurasQE_WebClient.Pages.Student;

[Authorize(Roles = "student")]
public class ProfileModel : PageModel
{
    private HttpClient Client;
    private readonly string StudentRoute;

    [BindProperty]
    public StudentDto Student { get; set; } = new();

    public List<SelectListItem> Genders { get; set; }
    public List<SelectListItem> Countries { get; set; }
    public List<SelectListItem> Neurodivergencies { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public ProfileModel(HttpClient http, IConfiguration configuration)
    {
        Client = http;
        StudentRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/students/");
    }

    private void InitSelects()
    {
        Genders =
        [
            new SelectListItem("Masculino", "M"),
            new SelectListItem("Femenino", "F"),
            new SelectListItem("Otro", "O")
        ];

        Countries =
        [
            new("México", "MX"),
            new("Estados Unidos", "US"),
            new("España", "ES")
        ];

        Neurodivergencies =
        [
            new("Autismo", "autismo"),
            new("TDA", "tda"),
            new("TDAH", "tdah"),
            new("Hiperactividad", "hiperactividad"),
            new("Ninguna", "ninguna"),
            new("Otra", "otra")
        ];
    }

    public async Task<IActionResult> OnGet()
    {
        InitSelects();

        var token = User.FindFirst("jwt_token")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
            return RedirectToPage("/User/Login");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{StudentRoute}{userId}"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Error al cargar perfil";
            return Page();
        }

        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        Student = student ?? new StudentDto();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        InitSelects();

        if (!ModelState.IsValid)
            return Page();

        var token = User.FindFirst("jwt_token")?.Value;

        if (string.IsNullOrEmpty(token))
        {
            ErrorMessage = "No autorizado";
            return Page();
        }

        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{StudentRoute}{Student.IdStudent}"
        );

        request.Content = new StringContent(
            JsonSerializer.Serialize(Student),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Error al actualizar el perfil";
            return Page();
        }

        var updated = await response.Content.ReadFromJsonAsync<StudentDto>();
        if (updated != null)
            Student = updated;

        SuccessMessage = "Perfil actualizado correctamente";

        return Page();
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.HttpContext.Response.Headers["Pragma"] = "no-cache";
        context.HttpContext.Response.Headers["Expires"] = "0";
    }
}
