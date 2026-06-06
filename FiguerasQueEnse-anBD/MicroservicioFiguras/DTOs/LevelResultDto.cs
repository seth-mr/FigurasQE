using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class LevelResultDto
{
    public int IdResult { get; set; }
    public int IdSession { get; set; }
    public int IdLevel { get; set; }
    public int? FinishingTime { get; set; }
    public int? Attempts { get; set; }
    public int? Fails { get; set; }
    public bool? Completed { get; set; }
    public SessionBasicDto? Session { get; set; }
    public LevelBasicDto? Level { get; set; }
}

public class CreateLevelResultDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "IdSession must be greater than 0.")]
    public int IdSession { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "IdLevel must be greater than 0.")]
    public int IdLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "FinishingTime must be 0 or greater.")]
    public int? FinishingTime { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Attempts must be 0 or greater.")]
    public int? Attempts { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Fails must be 0 or greater.")]
    public int? Fails { get; set; }

    public bool? Completed { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Attempts.HasValue && Fails.HasValue && Fails > Attempts)
        {
            yield return new ValidationResult(
                "Fails cannot be greater than Attempts.",
                new[] { nameof(Fails), nameof(Attempts) });
        }

        if (Completed == true && !FinishingTime.HasValue)
        {
            yield return new ValidationResult(
                "FinishingTime is required when Completed is true.",
                new[] { nameof(FinishingTime) });
        }
    }
}

public class UpdateLevelResultDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "IdSession must be greater than 0.")]
    public int IdSession { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "IdLevel must be greater than 0.")]
    public int IdLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "FinishingTime must be 0 or greater.")]
    public int? FinishingTime { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Attempts must be 0 or greater.")]
    public int? Attempts { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Fails must be 0 or greater.")]
    public int? Fails { get; set; }

    public bool? Completed { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Attempts.HasValue && Fails.HasValue && Fails > Attempts)
        {
            yield return new ValidationResult(
                "Fails cannot be greater than Attempts.",
                new[] { nameof(Fails), nameof(Attempts) });
        }

        if (Completed == true && !FinishingTime.HasValue)
        {
            yield return new ValidationResult(
                "FinishingTime is required when Completed is true.",
                new[] { nameof(FinishingTime) });
        }
    }
}
