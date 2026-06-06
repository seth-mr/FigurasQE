using System.Windows;
using FQE.AdminClient.Controllers;
using FQE.AdminClient.Services;
using FQE.AdminClient.Helpers;

namespace FQE.AdminClient;

public partial class App : Application
{
	private AdminApiClient? _apiClient;
	private AdminLogsApiClient? _logsApiClient;
	private readonly AdminSessionState _sessionState = new();

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var config = ConfigHelper.LoadConfig();
		_apiClient = new AdminApiClient(config.GatewayUrl);
		_logsApiClient = new AdminLogsApiClient(config.LogsApiUrl);

		var loginController = new LoginController(_apiClient, _sessionState);
		var welcomeController = new WelcomeController(_sessionState);
		var dashboardController = new DashboardController(_apiClient, _sessionState);
		var profileController = new ProfileController(_apiClient, _sessionState);
		var logsController = new LogsController(_logsApiClient, _sessionState);

		var loginWindow = new MainWindow(loginController, welcomeController, dashboardController, profileController, logsController);
		MainWindow = loginWindow;
		loginWindow.Show();
	}
}

