namespace FigurasQE_WebClient.Models;

public class SessionDto
{
    public int IdSession { get; set; }

    public int IdStudent { get; set; }

    public DateTime? BeginningDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Device { get; set; }

    public StudentBasicDto? Student { get; set; }

    public List<LevelResultBasicDto> LevelResults { get; set; } = [];
}

public class StudentBasicDto
{
    public int IdStudent { get; set; }

    public int? IdTutor { get; set; }

    public string? Name { get; set; }

    public int Age { get; set; }

    public char Gender { get; set; }

    public string? Country { get; set; }
}

public class LevelResultBasicDto
{
    public int IdResult { get; set; }

    public int IdLevel { get; set; }

    public int IdSession { get; set; }

    public bool? Completed { get; set; }
}

public class AssignStudentRequest
{
    public string StudentEmail { get; set; } = string.Empty;

    public string TutorEmail { get; set; } = string.Empty;
}

public class CreateGameSessionRequest
{
    public int IdStudent { get; set; }

    public DateTime? BeginningDate { get; set; }

    public string? Device { get; set; }
}

public class CreateLevelResultRequest
{
    public int IdSession { get; set; }

    public int IdLevel { get; set; }

    public int? FinishingTime { get; set; }

    public int? Attempts { get; set; }

    public int? Fails { get; set; }

    public bool? Completed { get; set; }
}
