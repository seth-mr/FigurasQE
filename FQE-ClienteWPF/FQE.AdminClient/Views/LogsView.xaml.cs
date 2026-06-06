using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Controls;
using FQE.AdminClient.Controllers;
using FQE.AdminClient.Models;
using FQE.AdminClient.Services;

namespace FQE.AdminClient.Views;

public partial class LogsView : UserControl
{
    private const string AllServicesLabel = "Todos los servicios";
    private const string AllTypesLabel = "Todos los tipos";
    private const string AllEntityTypesLabel = "Todas las entidades";
    private const string AllActionsLabel = "Todas las acciones";
    private const string AllStatusClassesLabel = "Todos los estados";
    private const int MaxReconnectDelaySeconds = 10;

    private readonly LogsController _logsController;
    private readonly Action _goBack;
    private readonly ObservableCollection<AdminLogEvent> _logs = [];
    private readonly HashSet<string> _knownIds = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _streamCancellation;
    private bool _hasLoaded;
    private bool _isFilterInitializing;
    private int _streamVersion;
    private AdminLogFilterCatalog _filterCatalog = new();
    private readonly SolidColorBrush _connectedBrush = new(Color.FromRgb(34, 197, 94));
    private readonly SolidColorBrush _disconnectedBrush = new(Color.FromRgb(220, 38, 38));

    public LogsView(LogsController logsController, Action goBack)
    {
        _logsController = logsController;
        _goBack = goBack;

        InitializeComponent();
        LogsDataGrid.ItemsSource = _logs;
        SetConnectionIndicator(false);

        Loaded += LogsView_Loaded;
        Unloaded += LogsView_Unloaded;
    }

    private async void LogsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await InitializeAsync();
    }

    private void LogsView_Unloaded(object sender, RoutedEventArgs e)
    {
        StopStreaming();
    }

    private async Task InitializeAsync()
    {
        SetBusyState(true);

        try
        {
            LoadServices();
            await ReloadLogsAsync();
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

    private void LoadServices()
    {
        _isFilterInitializing = true;

        try
        {
            _filterCatalog = AdminLogFilterCatalogProvider.Load();

            ServiceFilterComboBox.ItemsSource = CreateOptions(AllServicesLabel, _filterCatalog.Services);
            TypeFilterComboBox.ItemsSource = CreateOptions(AllTypesLabel, _filterCatalog.Types);
            EntityTypeFilterComboBox.ItemsSource = CreateOptions(AllEntityTypesLabel, _filterCatalog.EntityTypes);
            ActionFilterComboBox.ItemsSource = CreateOptions(AllActionsLabel, _filterCatalog.Actions);
            StatusClassFilterComboBox.ItemsSource = CreateOptions(AllStatusClassesLabel, _filterCatalog.StatusClasses);

            ServiceFilterComboBox.SelectedIndex = 0;
            TypeFilterComboBox.SelectedIndex = 0;
            EntityTypeFilterComboBox.SelectedIndex = 0;
            ActionFilterComboBox.SelectedIndex = 0;
            StatusClassFilterComboBox.SelectedIndex = 0;
            RouteFilterTextBox.Text = string.Empty;
        }
        finally
        {
            _isFilterInitializing = false;
        }
    }

    private async Task ReloadLogsAsync()
    {
        var service = GetSelectedService();
        var logType = GetSelectedType();
        var entityType = GetSelectedEntityType();
        var action = GetSelectedAction();
        var statusClass = GetSelectedStatusClass();
        var route = GetRouteFilter();

        var logs = await _logsController.GetLogsAsync(service, logType, entityType, action, statusClass, route);

        _knownIds.Clear();
        _logs.Clear();

        foreach (var log in logs.OrderByDescending(entry => entry.Timestamp))
        {
            InsertLogAtTop(log);
        }

        TotalLogsText.Text = _logs.Count.ToString();
        UpdateStatusMessage();
        StartStreaming();
    }

    private static List<string> CreateOptions(string allLabel, IEnumerable<string> values)
    {
        var options = new List<string> { allLabel };
        options.AddRange(values);
        return options;
    }

    private string? GetSelectedService()
    {
        if (ServiceFilterComboBox.SelectedItem is not string selected || selected == AllServicesLabel)
        {
            return null;
        }

        return selected;
    }

    private string? GetSelectedType()
    {
        if (TypeFilterComboBox.SelectedItem is not string selected || selected == AllTypesLabel)
        {
            return null;
        }

        return selected;
    }

    private string? GetSelectedEntityType()
    {
        if (EntityTypeFilterComboBox.SelectedItem is not string selected || selected == AllEntityTypesLabel)
        {
            return null;
        }

        return selected;
    }

    private string? GetSelectedAction()
    {
        if (ActionFilterComboBox.SelectedItem is not string selected || selected == AllActionsLabel)
        {
            return null;
        }

        return selected;
    }

    private string? GetSelectedStatusClass()
    {
        if (StatusClassFilterComboBox.SelectedItem is not string selected || selected == AllStatusClassesLabel)
        {
            return null;
        }

        return selected;
    }

    private string? GetRouteFilter()
    {
        var value = RouteFilterTextBox.Text?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void InsertLogAtTop(AdminLogEvent log)
    {
        if (string.IsNullOrWhiteSpace(log.Id) || !_knownIds.Add(log.Id))
        {
            return;
        }

        _logs.Insert(0, log);
        TotalLogsText.Text = _logs.Count.ToString();
    }

    private void StartStreaming()
    {
        StopStreaming();

        var streamVersion = ++_streamVersion;
        _streamCancellation = new CancellationTokenSource();
        var cancellationToken = _streamCancellation.Token;
        var service = GetSelectedService();
        var logType = GetSelectedType();
        var entityType = GetSelectedEntityType();
        var action = GetSelectedAction();
        var statusClass = GetSelectedStatusClass();
        var route = GetRouteFilter();
        SetConnectionIndicator(false);

        _ = Task.Run(async () =>
        {
            var reconnectAttempt = 0;

            while (!cancellationToken.IsCancellationRequested && streamVersion == _streamVersion)
            {
                try
                {
                    await _logsController.ListenAsync(
                        service,
                        logType,
                        entityType,
                        action,
                        statusClass,
                        route,
                        HandleIncomingLog,
                        isConnected => HandleConnectionChanged(streamVersion, isConnected),
                        cancellationToken);

                    if (cancellationToken.IsCancellationRequested || streamVersion != _streamVersion)
                    {
                        break;
                    }

                    reconnectAttempt += 1;
                    await NotifyReconnectAsync(streamVersion, reconnectAttempt, "La conexion en tiempo real se cerro.");
                    await Task.Delay(GetReconnectDelay(reconnectAttempt), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    reconnectAttempt += 1;
                    await NotifyReconnectAsync(streamVersion, reconnectAttempt, $"La conexion en tiempo real se corto: {ex.Message}");
                    await Task.Delay(GetReconnectDelay(reconnectAttempt), cancellationToken);
                }
            }
        }, cancellationToken);
    }

    private void StopStreaming()
    {
        if (_streamCancellation is null)
        {
            return;
        }

        _streamVersion += 1;
        _streamCancellation.Cancel();
        _streamCancellation.Dispose();
        _streamCancellation = null;
        SetConnectionIndicator(false);
    }

    private void HandleIncomingLog(AdminLogEvent log)
    {
        Dispatcher.Invoke(() =>
        {
            InsertLogAtTop(log);
            UpdateStatusMessage();
        });
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusyState(true);

        try
        {
            await ReloadLogsAsync();
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

    private async void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isFilterInitializing || !_hasLoaded)
        {
            return;
        }

        SetBusyState(true);

        try
        {
            await ReloadLogsAsync();
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

    private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        _isFilterInitializing = true;

        try
        {
            ServiceFilterComboBox.SelectedIndex = 0;
            TypeFilterComboBox.SelectedIndex = 0;
            EntityTypeFilterComboBox.SelectedIndex = 0;
            ActionFilterComboBox.SelectedIndex = 0;
            StatusClassFilterComboBox.SelectedIndex = 0;
            RouteFilterTextBox.Text = string.Empty;
        }
        finally
        {
            _isFilterInitializing = false;
        }

        SetBusyState(true);

        try
        {
            await ReloadLogsAsync();
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

    private void HandleConnectionChanged(int streamVersion, bool isConnected)
    {
        Dispatcher.Invoke(() =>
        {
            if (streamVersion != _streamVersion)
            {
                return;
            }

            SetConnectionIndicator(isConnected);

            if (isConnected)
            {
                UpdateStatusMessage();
            }
        });
    }

    private async Task NotifyReconnectAsync(int streamVersion, int reconnectAttempt, string reason)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (streamVersion != _streamVersion)
            {
                return;
            }

            SetConnectionIndicator(false);
            StatusText.Text = $"{reason} Reintentando conexion en {GetReconnectDelay(reconnectAttempt).TotalSeconds:0} segundos. Intento {reconnectAttempt}.";
        });
    }

    private static TimeSpan GetReconnectDelay(int reconnectAttempt)
    {
        var delaySeconds = Math.Min(Math.Max(1, reconnectAttempt * 2), MaxReconnectDelaySeconds);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        StopStreaming();
        _goBack();
    }

    private void SetBusyState(bool isBusy)
    {
        LogsDataGrid.IsEnabled = !isBusy;
        ServiceFilterComboBox.IsEnabled = !isBusy;
        TypeFilterComboBox.IsEnabled = !isBusy;
        EntityTypeFilterComboBox.IsEnabled = !isBusy;
        ActionFilterComboBox.IsEnabled = !isBusy;
        StatusClassFilterComboBox.IsEnabled = !isBusy;
        RouteFilterTextBox.IsEnabled = !isBusy;
    }

    private void UpdateStatusMessage()
    {
        var service = GetSelectedService();
        var logType = GetSelectedType();
        var entityType = GetSelectedEntityType();
        var action = GetSelectedAction();
        var statusClass = GetSelectedStatusClass();
        var route = GetRouteFilter();

        var filters = new List<string>();
        if (service is not null)
        {
            filters.Add($"servicio {service}");
        }

        if (logType is not null)
        {
            filters.Add($"tipo {logType}");
        }

        if (entityType is not null)
        {
            filters.Add($"entidad {entityType}");
        }

        if (action is not null)
        {
            filters.Add($"accion {action}");
        }

        if (statusClass is not null)
        {
            filters.Add($"estado {statusClass}");
        }

        if (route is not null)
        {
            filters.Add($"ruta contiene '{route}'");
        }

        var description = filters.Count == 0 ? "todos los logs" : string.Join(", ", filters);
        StatusText.Text = $"Vista en vivo con {description}.";
    }

    private void SetConnectionIndicator(bool isConnected)
    {
        ConnectionIndicator.Fill = isConnected ? _connectedBrush : _disconnectedBrush;
        ConnectionIndicator.Opacity = isConnected ? 1 : 0.95;
        ConnectionIndicatorText.Text = isConnected ? "Conexion activa" : "Sin conexion";

        if (Resources["ConnectionPulseStoryboard"] is not Storyboard storyboard)
        {
            return;
        }

        storyboard.Stop(this);
        ConnectionIndicatorScale.ScaleX = 1;
        ConnectionIndicatorScale.ScaleY = 1;

        if (isConnected)
        {
            storyboard.Begin(this, true);
        }
    }
}