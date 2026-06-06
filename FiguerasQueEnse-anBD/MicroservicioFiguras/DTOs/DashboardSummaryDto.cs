using System.Collections.Generic;

namespace MicroservicioFiguras.DTOs;

public class DashboardSummaryDto
{
    public DashboardTotalsDto Totals { get; set; } = new();

    public DashboardStudentsSummaryDto Students { get; set; } = new();

    public DashboardTutorsSummaryDto Tutors { get; set; } = new();

    public DashboardActivitySummaryDto Activity { get; set; } = new();
}

public class DashboardTotalsDto
{
    public int Students { get; set; }

    public int Tutors { get; set; }

    public int Admins { get; set; }

    public int Registered { get; set; }
}

public class DashboardStudentsSummaryDto
{
    public int WithNeurodivergency { get; set; }

    public double AverageAge { get; set; }

    public int MinimumAge { get; set; }

    public int MaximumAge { get; set; }

    public List<DashboardBreakdownDto> Gender { get; set; } = [];

    public List<DashboardBreakdownDto> Neurodivergency { get; set; } = [];
}

public class DashboardTutorsSummaryDto
{
    public double AverageAge { get; set; }

    public int MinimumAge { get; set; }

    public int MaximumAge { get; set; }

    public List<DashboardBreakdownDto> Gender { get; set; } = [];

    public List<DashboardBreakdownDto> Degree { get; set; } = [];

    public double AverageStudentsPerTutor { get; set; }
}

public class DashboardActivitySummaryDto
{
    public int RegisteredToday { get; set; }

    public int RegisteredThisWeek { get; set; }

    public int RegisteredThisMonth { get; set; }

    public double TotalHoursPlayed { get; set; }

    public int TotalLevelsSuperados { get; set; }
}

public class DashboardBreakdownDto
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}