namespace FQE.AdminClient.Models;

public class DashboardSummaryResponse
{
    public DashboardTotals? Totals { get; set; }

    public DashboardStudentsSummary? Students { get; set; }

    public DashboardTutorsSummary? Tutors { get; set; }

    public DashboardActivitySummary? Activity { get; set; }
}

public class DashboardTotals
{
    public int Students { get; set; }

    public int Tutors { get; set; }

    public int Admins { get; set; }

    public int Registered { get; set; }
}

public class DashboardStudentsSummary
{
    public int WithNeurodivergency { get; set; }

    public double AverageAge { get; set; }

    public int MinimumAge { get; set; }

    public int MaximumAge { get; set; }

    public List<DashboardBreakdownItem> Gender { get; set; } = [];

    public List<DashboardBreakdownItem> Neurodivergency { get; set; } = [];
}

public class DashboardTutorsSummary
{
    public double AverageAge { get; set; }

    public int MinimumAge { get; set; }

    public int MaximumAge { get; set; }

    public List<DashboardBreakdownItem> Gender { get; set; } = [];

    public List<DashboardBreakdownItem> Degree { get; set; } = [];

    public double AverageStudentsPerTutor { get; set; }
}

public class DashboardActivitySummary
{
    public int RegisteredToday { get; set; }

    public int RegisteredThisWeek { get; set; }

    public int RegisteredThisMonth { get; set; }

    public double TotalHoursPlayed { get; set; }

    public int TotalLevelsSuperados { get; set; }
}

public class DashboardBreakdownItem
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}
