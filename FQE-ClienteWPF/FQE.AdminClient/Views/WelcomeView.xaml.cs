using System.Windows;
using System.Windows.Controls;
using FQE.AdminClient.Controllers;

namespace FQE.AdminClient.Views;

public partial class WelcomeView : UserControl
{
    private readonly LoginController _loginController;
    private readonly WelcomeController _welcomeController;
    private readonly Action _openAccount;
    private readonly Action _openLogs;
    private readonly Action _openServiceMonitor;
    private readonly Action _openStatistics;
    private readonly Action _showLogin;

    public WelcomeView(LoginController loginController, WelcomeController welcomeController, Action openAccount, Action openLogs, Action openServiceMonitor, Action openStatistics, Action showLogin)
    {
        _loginController = loginController;
        _welcomeController = welcomeController;
        _openAccount = openAccount;
        _openLogs = openLogs;
        _openServiceMonitor = openServiceMonitor;
        _openStatistics = openStatistics;
        _showLogin = showLogin;

        InitializeComponent();
        LoadWelcomeData();
    }

    private void LoadWelcomeData()
    {
        var admin = _welcomeController.GetCurrentAdmin();
        WelcomeTitleText.Text = _welcomeController.GetWelcomeTitle();
        AccountSummaryText.Text = $"Admin activo: {admin.Email}";
    }

    private void OpenAccountButton_Click(object sender, RoutedEventArgs e)
    {
        _openAccount();
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        _openLogs();
    }

    private void OpenServiceMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        _openServiceMonitor();
    }

    private void OpenStatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        _openStatistics();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _loginController.Logout();
        _showLogin();
    }
}