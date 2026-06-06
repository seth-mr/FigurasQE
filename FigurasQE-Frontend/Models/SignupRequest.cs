using System.ComponentModel.DataAnnotations;

namespace FigurasQE_WebClient.Models;

public class SignupRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Name { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    public string Email { get; set; }

    public string? Password { get; set; }

    [Required(ErrorMessage = "Selecciona un género")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Selecciona un país")]
    public string Country { get; set; }

    [Required(ErrorMessage = "La edad es obligatoria")]
    [Range(1, 85, ErrorMessage = "La edad debe estar entre 1 y 85")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Selecciona un tipo de usuario")]
    public string Role { get; set; }

    public string? Neurodivergency { get; set; }

    public string? Degree { get; set; }
}