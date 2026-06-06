using System.Net.Http.Headers;
using System.Text.Json;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FigurasQE_WebClient.Pages.Tutor;

[Authorize(Roles = "tutor")] // fix - revisar que sí se pueda entrar siempre con login
// corregir dto del back
public class ProfileModel : PageModel
{
    private HttpClient Client;
    private readonly string TutorRoute;

    [BindProperty]
    public TutorDto Tutor { get; set; } = new();

    public List<SelectListItem> Genres { get; set; }
    public List<SelectListItem> Countries { get; set; }
    public List<SelectListItem> Grades { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public ProfileModel(HttpClient http, IConfiguration configuration)
    {
        Client = http;
        TutorRoute = ApiGatewayRoutes.InternalUrl(configuration, "/data/tutors/");
    }

    private void InitSelects()
    {
        Genres = new()
        {
            new("Masculino", "M"),
            new("Femenino", "F"),
            new("Otro", "O")
        };

        Countries = new()
        {
            new("México", "MX"),
            new("Estados Unidos", "US"),
            new("España", "ES")
        };

        Grades = new()
        {
            new("Licenciatura", "licenciatura"),
            new("Maestría", "maestria"),
            new("Doctorado", "doctorado"),
            new("Post Doctorado", "postdoctorado"),
            new("Padre o Madre", "padre-madre"),
        };
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
            $"{TutorRoute}{userId}"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Error al cargar perfil";
            return Page();
        }

        var tutor = await response.Content.ReadFromJsonAsync<TutorDto>();
        Tutor = tutor ?? new TutorDto();

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
            $"{TutorRoute}{Tutor.IdTutor}"
        );

        request.Content = new StringContent(
            JsonSerializer.Serialize(Tutor),
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

        var updated = await response.Content.ReadFromJsonAsync<TutorDto>();
        if (updated != null)
            Tutor = updated;

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
