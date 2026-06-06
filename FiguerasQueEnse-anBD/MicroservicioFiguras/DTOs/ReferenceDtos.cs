namespace MicroservicioFiguras.DTOs;

public class StudentBasicDto
{
    public int IdStudent { get; set; }
    public int? IdTutor { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public char Gender { get; set; }
    public string? Country { get; set; }
}

public class TutorBasicDto
{
    public int IdTutor { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; } = null!;
}

public class LevelBasicDto
{
    public int IdLevel { get; set; }
    public string? Name { get; set; }
    public int? Difficulty { get; set; }
}

public class SessionBasicDto
{
    public int IdSession { get; set; }
    public int IdStudent { get; set; }
    public string? Device { get; set; }
}

public class LevelResultBasicDto
{
    public int IdResult { get; set; }
    public int IdLevel { get; set; }
    public int IdSession { get; set; }
    public bool? Completed { get; set; }
}
