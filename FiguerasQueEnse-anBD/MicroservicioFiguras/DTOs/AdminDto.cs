using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class AdminDto
{
    public int IdAdmin { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
}

public class CreateAdminDto
{
    [Required]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Phone must contain 7 to 20 digits or dialing characters.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Za-z0-9._-]{3,60}$", ErrorMessage = "Username must be 3 to 60 characters and use letters, numbers, dot, underscore or hyphen.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class UpdateAdminDto
{
    [Required]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Phone must contain 7 to 20 digits or dialing characters.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Za-z0-9._-]{3,60}$", ErrorMessage = "Username must be 3 to 60 characters and use letters, numbers, dot, underscore or hyphen.")]
    public string Username { get; set; } = string.Empty;

    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters.")]
    public string? Password { get; set; }
}