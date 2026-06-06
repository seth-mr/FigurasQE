using System.ComponentModel.DataAnnotations;

namespace FigurasQE_AuthenticationService.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Age is required")]
    [Range(1, 120, ErrorMessage = "Age must be between 1 and 85")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [RegularExpression("^[MFO]$", ErrorMessage = "Gender must be M, F or O")]
    public char Gender { get; set; }

    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be ISO code (e.g. MX, US)")]
    public string Country { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(student|tutor)$", ErrorMessage = "Role must be student or tutor")]
    public string Role { get; set; } = string.Empty;

    [RegularExpression(@"^[\p{L}0-9\s\-_]*$", ErrorMessage = "Neurodivergency may contain letters, numbers, spaces, hyphens and underscores.")]
    public string? Neurodivergency { get; set; }

    public string? Degree { get; set; }
}