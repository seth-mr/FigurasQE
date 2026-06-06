using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroservicioFiguras.DTOs;
using MicroservicioFiguras.Interfaces;
using MicroservicioFiguras.Models;

namespace MicroservicioFiguras.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly FigurasqeContext _context;

    public DashboardRepository(FigurasqeContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var students = await _context.Students
            .AsNoTracking()
            .Select(student => new StudentDashboardRow(
                student.IdTutor,
                student.Age,
                student.Gender,
                student.Neurodivergency,
                student.RegistrationDate))
            .ToListAsync();

        var tutors = await _context.Tutors
            .AsNoTracking()
            .Select(tutor => new TutorDashboardRow(
                tutor.IdTutor,
                tutor.Age,
                tutor.Gender,
                tutor.Degree,
                tutor.RegistrationDate))
            .ToListAsync();

        var admins = await _context.Admins
            .AsNoTracking()
            .Select(admin => new AdminDashboardRow(admin.RegistrationDate))
            .ToListAsync();

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Select(session => new SessionDashboardRow(session.BeginningDate, session.EndDate))
            .ToListAsync();

        var levelResults = await _context.LevelResults
            .AsNoTracking()
            .Select(result => result.Completed)
            .ToListAsync();

        return new DashboardSummaryDto
        {
            Totals = new DashboardTotalsDto
            {
                Students = students.Count,
                Tutors = tutors.Count,
                Admins = admins.Count,
                Registered = students.Count + tutors.Count + admins.Count
            },
            Students = BuildStudentsSummary(students),
            Tutors = BuildTutorsSummary(students, tutors),
            Activity = BuildActivitySummary(students, tutors, admins, sessions, levelResults)
        };
    }

    private static DashboardStudentsSummaryDto BuildStudentsSummary(IReadOnlyCollection<StudentDashboardRow> students)
    {
        var ages = students.Select(student => student.Age).ToList();

        return new DashboardStudentsSummaryDto
        {
            WithNeurodivergency = students.Count(student => !string.IsNullOrWhiteSpace(student.Neurodivergency)),
            AverageAge = ages.Count == 0 ? 0 : Math.Round(ages.Average(), 2),
            MinimumAge = ages.Count == 0 ? 0 : ages.Min(),
            MaximumAge = ages.Count == 0 ? 0 : ages.Max(),
            Gender = BuildBreakdown(students.Select(student => NormalizeGender(student.Gender))),
            Neurodivergency = BuildBreakdown(students.Select(student => NormalizeText(student.Neurodivergency)))
        };
    }

    private static DashboardTutorsSummaryDto BuildTutorsSummary(IReadOnlyCollection<StudentDashboardRow> students, IReadOnlyCollection<TutorDashboardRow> tutors)
    {
        var tutorAges = tutors
            .Where(tutor => tutor.Age.HasValue)
            .Select(tutor => tutor.Age!.Value)
            .ToList();

        var studentsPerTutor = tutors
            .Select(tutor => students.Count(student => student.IdTutor.HasValue && student.IdTutor.Value == tutor.IdTutor))
            .ToList();

        return new DashboardTutorsSummaryDto
        {
            AverageAge = tutorAges.Count == 0 ? 0 : Math.Round(tutorAges.Average(), 2),
            MinimumAge = tutorAges.Count == 0 ? 0 : tutorAges.Min(),
            MaximumAge = tutorAges.Count == 0 ? 0 : tutorAges.Max(),
            Gender = BuildBreakdown(tutors.Select(tutor => NormalizeGender(tutor.Gender))),
            Degree = BuildBreakdown(tutors.Select(tutor => NormalizeText(tutor.Degree))),
            AverageStudentsPerTutor = studentsPerTutor.Count == 0 ? 0 : Math.Round(studentsPerTutor.Average(), 2)
        };
    }

    private static DashboardActivitySummaryDto BuildActivitySummary(
        IReadOnlyCollection<StudentDashboardRow> students,
        IReadOnlyCollection<TutorDashboardRow> tutors,
        IReadOnlyCollection<AdminDashboardRow> admins,
        IReadOnlyCollection<SessionDashboardRow> sessions,
        IReadOnlyCollection<bool?> levelResults)
    {
        var registrations = students.Select(student => student.RegistrationDate)
            .Concat(tutors.Select(tutor => tutor.RegistrationDate))
            .Concat(admins.Select(admin => admin.RegistrationDate))
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToList();

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var validSessions = sessions
            .Where(session => session.BeginningDate.HasValue && session.EndDate.HasValue && session.EndDate > session.BeginningDate)
            .Select(session => (session.EndDate!.Value - session.BeginningDate!.Value).TotalHours)
            .ToList();

        return new DashboardActivitySummaryDto
        {
            RegisteredToday = registrations.Count(date => date >= today && date < today.AddDays(1)),
            RegisteredThisWeek = registrations.Count(date => date >= startOfWeek && date < startOfWeek.AddDays(7)),
            RegisteredThisMonth = registrations.Count(date => date >= startOfMonth && date < startOfMonth.AddMonths(1)),
            TotalHoursPlayed = Math.Round(validSessions.Sum(), 2),
            TotalLevelsSuperados = levelResults.Count(result => result == true)
        };
    }

    private static List<DashboardBreakdownDto> BuildBreakdown(IEnumerable<string> values)
    {
        return values
            .GroupBy(value => value)
            .Select(group => new DashboardBreakdownDto
            {
                Label = NormalizeText(group.Key),
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Sin dato" : value.Trim();
    }

    private static string NormalizeGender(char? gender)
    {
        if (!gender.HasValue)
        {
            return "Sin dato";
        }

        return char.ToUpperInvariant(gender.Value) switch
        {
            'M' => "M",
            'F' => "F",
            'O' => "O",
            _ => gender.Value.ToString()
        };
    }

    private sealed record StudentDashboardRow(int? IdTutor, int Age, char Gender, string? Neurodivergency, DateTime? RegistrationDate);

    private sealed record TutorDashboardRow(int IdTutor, int? Age, char? Gender, string? Degree, DateTime? RegistrationDate);

    private sealed record AdminDashboardRow(DateTime? RegistrationDate);

    private sealed record SessionDashboardRow(DateTime? BeginningDate, DateTime? EndDate);
}