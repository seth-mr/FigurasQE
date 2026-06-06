using System;
using System.Collections.Generic;

namespace FigurasQE_WebClient.Models;

public class TutorDto
{
    public int IdTutor { get; set; }

    public string Name { get; set; } = null!;

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string? Country { get; set; }

    public string? Degree { get; set; }

    public string? Email { get; set; }
}
