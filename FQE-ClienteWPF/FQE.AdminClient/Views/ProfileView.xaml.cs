using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FQE.AdminClient.Controllers;
using FQE.AdminClient.Models;

namespace FQE.AdminClient.Views;

public partial class ProfileView : UserControl
{
    private readonly ProfileController _profileController;
    private readonly Action _goBack;
    private AdminProfile? _profile;

    public ProfileView(ProfileController profileController, Action goBack)
    {
        _profileController = profileController;
        _goBack = goBack;

        InitializeComponent();
        LoadCachedProfile();
    }

    private void LoadCachedProfile()
    {
        _profile = _profileController.GetCachedProfile();
        FillForm(_profile);
        StatusText.Text = "";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusyState(true);

        try
        {
            _profile = await _profileController.RefreshAsync();
            FillForm(_profile);
            NewPasswordBox.Clear();
            StatusText.Text = "Datos recargados desde el servidor.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs())
        {
            return;
        }

        SetBusyState(true);

        try
        {
            var request = new UpdateAdminRequest
            {
                Name = NameTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Username = UsernameTextBox.Text.Trim(),
                Password = string.IsNullOrWhiteSpace(NewPasswordBox.Password) ? null : NewPasswordBox.Password
            };

            _profile = await _profileController.SaveAsync(request);
            FillForm(_profile);
            NewPasswordBox.Clear();
            StatusText.Text = "Cambios guardados correctamente.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null)
        {
            return;
        }

        FillForm(_profile);
        NewPasswordBox.Clear();
        StatusText.Text = "Formulario restaurado con los ultimos datos disponibles.";
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _goBack();
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
            string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
            string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
            string.IsNullOrWhiteSpace(UsernameTextBox.Text))
        {
            StatusText.Text = "Nombre, correo, telefono y usuario son obligatorios.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password) && NewPasswordBox.Password.Length < 8)
        {
            StatusText.Text = "La nueva contrasenia debe tener al menos 8 caracteres.";
            return false;
        }

        return true;
    }

    private void FillForm(AdminProfile profile)
    {
        NameTextBox.Text = profile.Name;
        EmailTextBox.Text = profile.Email;
        PhoneTextBox.Text = profile.Phone;
        UsernameTextBox.Text = profile.Username;
        RegistrationDateText.Text = profile.RegistrationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha";
    }

    private void SetBusyState(bool isBusy)
    {
        IsEnabled = !isBusy;
        Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
    }
}