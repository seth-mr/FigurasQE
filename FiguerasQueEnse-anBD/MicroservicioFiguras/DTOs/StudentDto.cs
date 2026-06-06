using System;

namespace MicroservicioFiguras.DTOs;

public class StudentDto
{
    public int IdStudent { get; set; }
    public int? IdTutor { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public char Gender { get; set; }
    public string? Country { get; set; }
    public string? Neurodivergency { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public TutorDto? Tutor { get; set; }
}
