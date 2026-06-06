using System;
using System.Collections.Generic;

namespace FigurasQE_WebClient.Models;

public class StudentDto
{
    public int IdStudent { get; set; }

    public int? IdTutor { get; set; }

    public string Name { get; set; } = null!;

    public int Age { get; set; }

    public char Gender { get; set; }

    public string? Country { get; set; }

    public string? Neurodivergency { get; set; }

    public string? Email { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public TutorDto? Tutor { get; set; }
}
