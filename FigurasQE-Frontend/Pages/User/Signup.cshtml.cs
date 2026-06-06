using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FigurasQE_WebClient.Pages.User;

public class SignupModel : PageModel
{
    private readonly string UserRoute;

    [BindProperty]
    public SignupRequest Input { get; set; } = new SignupRequest();

    [BindProperty]
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{8,}$",
    ErrorMessage = "Debe tener mínimo 8 caracteres, mayúscula, minúscula y un número")]
    public string Password { get; set; }

    public List<SelectListItem> Countries { get; set; }
    public List<SelectListItem> Neurodivergencies { get; set; }
    public List<SelectListItem> Degrees { get; set; }

    public SignupModel(IConfiguration configuration)
    {
        UserRoute = ApiGatewayRoutes.InternalUrl(configuration, "/auth/register");
    }

    private void InitSelects()
    {
        Countries = new()
        {
            new("México", "MX"),
            new("Estados Unidos", "US"),
            new("España", "ES")
        };

        Neurodivergencies = new()
        {
            new("Autismo", "autismo"),
            new("TDA", "tda"),
            new("TDAH", "tdah"),
            new("Hiperactividad", "hiperactividad"),
            new("Ninguna", "ninguna"),
            new("Otra", "otra")
        };
        
        Degrees = new()
        {
            new("Licenciatura", "licenciatura"),
            new("Maestría", "maestria"),
            new("Doctorado", "doctorado"),
            new("Post Doctorado", "postdoctorado"),
            new("Padre o Madre", "padre-madre"),
        };
    }

    public void OnGet()
    {
        Input = new SignupRequest
        {
            Age = 5,
            Role = "student",
        };

        InitSelects();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        InitSelects();


        if (!ModelState.IsValid)
            return Page();

        Input.Password = Password;

        var json = System.Text.Json.JsonSerializer.Serialize(Input);
        Console.WriteLine("INPUT JSON:");
        Console.WriteLine(json);

        using var client = new HttpClient();


        var response = await client.PostAsJsonAsync(
            UserRoute,
            Input
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorJson);
            return Page();
        }

        return RedirectToPage("/User/Login");
    }
}

