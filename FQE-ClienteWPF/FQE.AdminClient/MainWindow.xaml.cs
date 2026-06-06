using System.Windows;
using FQE.AdminClient.Controllers;
using FQE.AdminClient.Views;

namespace FQE.AdminClient;

public partial class MainWindow : Window
{
    private readonly LoginController _loginController;
    private readonly WelcomeController _welcomeController;
    private readonly DashboardController _dashboardController;
    private readonly ProfileController _profileController;
    private readonly LogsController _logsController;

    public MainWindow(LoginController loginController, WelcomeController welcomeController, DashboardController dashboardController, ProfileController profileController, LogsController logsController)
    {
        _loginController = loginController;
        _welcomeController = welcomeController;
        _dashboardController = dashboardController;
        _profileController = profileController;
        _logsController = logsController;

        InitializeComponent();
        ShowLoginView();
    }

    private void ShowLoginView()
    {
        ViewHost.Content = new LoginView(_loginController, ShowWelcomeView);
    }

    private void ShowWelcomeView()
    {
        ViewHost.Content = new WelcomeView(_loginController, _welcomeController, ShowProfileView, ShowLogsView, ShowServiceMonitorView, ShowStatisticsView, ShowLoginView);
    }

    private void ShowStatisticsView()
    {
        ViewHost.Content = new StatisticsView(_dashboardController, ShowWelcomeView);
    }

    private void ShowProfileView()
    {
        ViewHost.Content = new ProfileView(_profileController, ShowWelcomeView);
    }

    private void ShowLogsView()
    {
        ViewHost.Content = new LogsView(_logsController, ShowWelcomeView);
    }

    private void ShowServiceMonitorView()
    {
        ViewHost.Content = new ServiceMonitorView(ShowWelcomeView);
    }

}