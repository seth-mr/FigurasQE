using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FQE.AdminClient.Controllers;

namespace FQE.AdminClient.Views;

public partial class LoginView : UserControl
{
    private readonly LoginController _loginController;
    private readonly Action _onLoginSuccess;

    public LoginView(LoginController loginController, Action onLoginSuccess)
    {
        _loginController = loginController;
        _onLoginSuccess = onLoginSuccess;

        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginStatusText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(LoginEmailTextBox.Text) || string.IsNullOrWhiteSpace(LoginPasswordBox.Password))
        {
            LoginStatusText.Text = "Captura correo y contrasenia para continuar.";
            return;
        }

        SetBusyState(true);

        try
        {
            await _loginController.LoginAsync(LoginEmailTextBox.Text.Trim(), LoginPasswordBox.Password);
            LoginPasswordBox.Clear();
            _onLoginSuccess();
        }
        catch (Exception ex)
        {
            LoginStatusText.Text = ex.Message;
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void SetBusyState(bool isBusy)
    {
        IsEnabled = !isBusy;
        Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
    }
}