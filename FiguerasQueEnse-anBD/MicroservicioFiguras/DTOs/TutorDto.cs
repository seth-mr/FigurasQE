using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class TutorDto
{
    public int IdTutor { get; set; }

    [StringLength(120, ErrorMessage = "Name must be 120 characters or fewer.")]
    public string? Name { get; set; }

    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must be a valid address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be ISO code (e.g. MX, US)")]
    public string Country { get; set; } = string.Empty;

    public char? Gender { get; set; }

    public int? Age { get; set; }

    public string? Degree { get; set; }

    public DateTime? RegistrationDate { get; set; }
    public List<StudentBasicDto>? Students { get; set; }
}

public class CreateTutorDto
{
    [Required]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters.")]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must be a valid address.")]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "PasswordHash must be between 8 and 255 characters.")]
    public string PasswordHash { get; set; } = null!;

    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be ISO code (e.g. MX, US)")]
    public string Country { get; set; } = string.Empty;

    [RegularExpression(@"^[MFO]$", ErrorMessage = "Gender must be 'M', 'F' or 'O'.")]
    public char? Gender { get; set; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
    public int? Age { get; set; }

    [RegularExpression("^(licenciatura|maestria|doctorado|postdoctorado|padre-madre|otro)$",
        ErrorMessage = "Degree must be one of: licenciatura, Maestria, Doctorado, Post Doctorado, Padre o Madre.")]
    public string? Degree { get; set; }
}

public class UpdateTutorDto
{
    [Required]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 120 characters.")]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress(ErrorMessage = "Email must be a valid address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be ISO code (e.g. MX, US)")]
    public string Country { get; set; } = string.Empty;

    [RegularExpression(@"^[MFO]$", ErrorMessage = "Gender must be 'M', 'F' or 'O'.")]
    public char? Gender { get; set; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
    public int? Age { get; set; }

    [RegularExpression("^(licenciatura|maestria|doctorado|postdoctorado|padre-madre|otro)$",
        ErrorMessage = "Degree must be one of: licenciatura, Maestria, Doctorado, Post Doctorado, Padre o Madre, Otro.")]
    public string? Degree { get; set; }
}
