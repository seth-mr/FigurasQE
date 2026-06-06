using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class CreateStudentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "IdTutor must be greater than 0.")]
    public int? IdTutor { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters.")]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must be a valid address.")]
    public string? Email { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "PasswordHash must be between 8 and 255 characters.")]
    public string? PasswordHash { get; set; }

    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120.")]
    public int Age { get; set; }

    [RegularExpression(@"^[MF]$", ErrorMessage = "Genre must be 'M' or 'F'.")]
    public char Gender { get; set; }

    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be ISO code (e.g. MX, US)")]
    public string Country { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Neurodivergency must be 50 characters or fewer.")]
    [RegularExpression(@"^[\p{L}0-9\s\-_]*$", ErrorMessage = "Neurodivergency may contain letters, numbers, spaces, hyphens and underscores.")]
    public string? Neurodivergency { get; set; }
}
