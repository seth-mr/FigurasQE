using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FigurasQE_WebClient.Models;
using FigurasQE_WebClient.Services;
using Microsoft.AspNetCore.Authentication;

namespace FigurasQE_WebClient.Pages.User;

public class LoginModel : PageModel
{
    private HttpClient Client;
    private readonly string LoginRoute;

    [BindProperty]
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    public string Email { get; set; }


    [BindProperty]
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; }

    public LoginModel(HttpClient client, IConfiguration configuration)
    {
        Client = client;
        LoginRoute = ApiGatewayRoutes.InternalUrl(configuration, "/auth/login");
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var response = await Client.PostAsJsonAsync(
            LoginRoute,
            new { Email, Password }
        );

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Credenciales Inválidas");
            return Page();
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        if (result == null || string.IsNullOrEmpty(result.Token))
        {
            ModelState.AddModelError(string.Empty, "Error procesando la respuesta del servidor");
            return Page();
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
            ?? jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

        var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError(string.Empty, "Token inválido");
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("jwt_token", result.Token),
            new Claim("sub", userId)
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);

        return role == "student"
            ? RedirectToPage("/Student/Home")
            : RedirectToPage("/Tutor/Home");
    }
}
