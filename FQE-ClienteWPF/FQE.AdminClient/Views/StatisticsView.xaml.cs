using System.Windows;
using System.Windows.Controls;
using FQE.AdminClient.Controllers;
using FQE.AdminClient.Models;

namespace FQE.AdminClient.Views;

public partial class StatisticsView : UserControl
{
    private const double MaxBarWidth = 220;

    private readonly DashboardController _dashboardController;
    private readonly Action _goBack;

    public StatisticsView(DashboardController dashboardController, Action goBack)
    {
        _dashboardController = dashboardController;
        _goBack = goBack;

        InitializeComponent();
        Loaded += StatisticsView_Loaded;
    }

    private async void StatisticsView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var summary = await _dashboardController.GetSummaryAsync();
            PaintSummary(summary);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Estadisticas", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PaintSummary(DashboardSummaryResponse summary)
    {
        var totals = summary.Totals ?? new DashboardTotals();
        var students = summary.Students ?? new DashboardStudentsSummary();
        var tutors = summary.Tutors ?? new DashboardTutorsSummary();
        var activity = summary.Activity ?? new DashboardActivitySummary();

        TotalRegisteredText.Text = totals.Registered.ToString();
        StudentsTotalText.Text = totals.Students.ToString();
        TutorsTotalText.Text = totals.Tutors.ToString();
        AverageStudentsPerTutorText.Text = tutors.AverageStudentsPerTutor.ToString("0.00");

        RegisteredTodayText.Text = activity.RegisteredToday.ToString();
        RegisteredWeekText.Text = activity.RegisteredThisWeek.ToString();
        RegisteredMonthText.Text = activity.RegisteredThisMonth.ToString();
        HoursPlayedText.Text = activity.TotalHoursPlayed.ToString("0.00");
        LevelsSuperadosText.Text = activity.TotalLevelsSuperados.ToString();

        StudentGenderItemsControl.ItemsSource = BuildBars(students.Gender);
        StudentNeuroItemsControl.ItemsSource = BuildBars(students.Neurodivergency);
        TutorDegreeItemsControl.ItemsSource = BuildBars(tutors.Degree);
        TutorGenderItemsControl.ItemsSource = BuildBars(tutors.Gender);
    }

    private static List<DashboardBarItem> BuildBars(IReadOnlyCollection<DashboardBreakdownItem> source)
    {
        var max = source.Count == 0 ? 0 : source.Max(item => item.Count);

        return source.Select(item => new DashboardBarItem
        {
            Label = item.Label,
            Count = item.Count,
            BarWidth = max == 0 ? 0 : Math.Max(10, MaxBarWidth * item.Count / (double)max)
        }).ToList();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _goBack();
    }

    private sealed class DashboardBarItem
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public double BarWidth { get; set; }
    }
}
