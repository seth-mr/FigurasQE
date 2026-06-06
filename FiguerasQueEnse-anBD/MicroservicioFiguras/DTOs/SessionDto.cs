using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class SessionDto
{
    public int IdSession { get; set; }
    public int IdStudent { get; set; }
    public DateTime? BeginningDate { get; set; }
    public DateTime? EndDate { get; set; }

    [RegularExpression(@"^[\p{L}0-9\s\-_]{0,50}$", ErrorMessage = "Device may contain letters, numbers, spaces, hyphens and underscores.")]
    public string? Device { get; set; }
    public StudentBasicDto? Student { get; set; }
    public List<LevelResultBasicDto>? LevelResults { get; set; }
}

public class CreateSessionDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "IdStudent must be greater than 0.")]
    public int IdStudent { get; set; }
    public DateTime? BeginningDate { get; set; }
    public DateTime? EndDate { get; set; }

    [RegularExpression(@"^[\p{L}0-9\s\-_]{0,50}$", ErrorMessage = "Device may contain letters, numbers, spaces, hyphens and underscores.")]
    public string? Device { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var serverNow = DateTime.Now;

        if (BeginningDate.HasValue && BeginningDate.Value > serverNow)
        {
            yield return new ValidationResult(
                "BeginningDate cannot be in the future.",
                new[] { nameof(BeginningDate) });
        }

        if (BeginningDate.HasValue && EndDate.HasValue && EndDate < BeginningDate)
        {
            yield return new ValidationResult(
                "EndDate must be the same or later than BeginningDate.",
                new[] { nameof(EndDate), nameof(BeginningDate) });
        }
    }
}

public class UpdateSessionDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "IdStudent must be greater than 0.")]
    public int IdStudent { get; set; }
    public DateTime? BeginningDate { get; set; }
    public DateTime? EndDate { get; set; }

    [RegularExpression(@"^[\p{L}0-9\s\-_]{0,50}$", ErrorMessage = "Device may contain letters, numbers, spaces, hyphens and underscores.")]
    public string? Device { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var serverNow = DateTime.Now;

        if (BeginningDate.HasValue && BeginningDate.Value > serverNow)
        {
            yield return new ValidationResult(
                "BeginningDate cannot be in the future.",
                new[] { nameof(BeginningDate) });
        }

        if (BeginningDate.HasValue && EndDate.HasValue && EndDate < BeginningDate)
        {
            yield return new ValidationResult(
                "EndDate must be the same or later than BeginningDate.",
                new[] { nameof(EndDate), nameof(BeginningDate) });
        }
    }
}
