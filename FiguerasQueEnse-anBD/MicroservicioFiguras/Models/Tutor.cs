using System;
using System.Collections.Generic;

namespace MicroservicioFiguras.Models;

public partial class Tutor
{
    public int IdTutor { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Country { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public string? Name { get; set; }

    public char? Gender { get; set; }

    public int? Age { get; set; }

    public string? Degree { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
