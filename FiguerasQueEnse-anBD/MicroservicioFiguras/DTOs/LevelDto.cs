using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioFiguras.DTOs;

public class LevelDto
{
    public int IdLevel { get; set; }
    public string? Name { get; set; }
    public int? Difficulty { get; set; }
    public List<LevelResultBasicDto>? LevelResults { get; set; }
}

public class CreateLevelDto
{
    [Required]
    [RegularExpression(@"^[\p{L}0-9\s\-]{1,100}$", ErrorMessage = "Name must be 1-100 letters, numbers, spaces or hyphens.")]
    public string? Name { get; set; }

    [Range(1, 10, ErrorMessage = "Difficulty must be between 1 and 10.")]
    public int? Difficulty { get; set; }
}

public class UpdateLevelDto
{
    [Required]
    [RegularExpression(@"^[\p{L}0-9\s\-]{1,100}$", ErrorMessage = "Name must be 1-100 letters, numbers, spaces or hyphens.")]
    public string? Name { get; set; }

    [Range(1, 10, ErrorMessage = "Difficulty must be between 1 and 10.")]
    public int? Difficulty { get; set; }
}
