using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class AssignTutorDto : IValidatableObject
{
    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "StudentEmail must be a valid address.")]
    public string StudentEmail { get; set; } = null!;

    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "TutorEmail must be a valid address.")]
    public string TutorEmail { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(StudentEmail, TutorEmail, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "StudentEmail and TutorEmail must be different.",
                new[] { nameof(StudentEmail), nameof(TutorEmail) });
        }
    }
}
