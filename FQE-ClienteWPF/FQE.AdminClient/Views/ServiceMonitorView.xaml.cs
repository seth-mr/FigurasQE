using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FQE.AdminClient.Views;

public partial class ServiceMonitorView : UserControl
{
    private const int MinimumHeartbeatSeconds = 5;
    private const int MaximumHeartbeatSeconds = 300;

    private static readonly HttpClient HealthClient = new();
    private static readonly SolidColorBrush HealthyHeartbeatBrush = CreateFrozenBrush(Color.FromRgb(34, 197, 94));
    private static readonly SolidColorBrush UnhealthyHeartbeatBrush = CreateFrozenBrush(Color.FromRgb(220, 38, 38));
    private static readonly SolidColorBrush NeutralHeartbeatBrush = CreateFrozenBrush(Color.FromRgb(100, 116, 139));

    private readonly DispatcherTimer _heartbeatTimer;
    private readonly List<ServiceMonitorCard> _cards;
    private readonly Action _goBack;
    private bool _isRefreshing;

    public ServiceMonitorView(Action goBack)
    {
        _goBack = goBack;
        _cards = CreateCards();
        _heartbeatTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _heartbeatTimer.Tick += HeartbeatTimer_Tick;

        InitializeComponent();
        ServicesItemsControl.ItemsSource = _cards;

        Loaded += ServiceMonitorView_Loaded;
        Unloaded += ServiceMonitorView_Unloaded;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _goBack();
    }

    private async void ServiceMonitorView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshHeartbeatAsync();
        _heartbeatTimer.Start();
    }

    private void ServiceMonitorView_Unloaded(object sender, RoutedEventArgs e)
    {
        _heartbeatTimer.Stop();
    }

    private async void HeartbeatTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshHeartbeatAsync();
    }

    private async Task RefreshHeartbeatAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;

        try
        {
            var tasks = _cards.Select(UpdateCardHeartbeatAsync).ToArray();
            await Task.WhenAll(tasks);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private static List<ServiceMonitorCard> CreateCards()
    {
        return
        [
            new("MongoDB", "http://localhost:3000/health/mongo", "Heartbeat de MongoDB consultado por gateway a traves de LogsService."),
            new("Rabbit Listener", "http://localhost:3000/health/rabbit-listener", "Worker que consume RabbitMQ y persiste eventos en MongoDB para el flujo de logs."),
            new("Auth Service", "http://localhost:3000/health/auth", "Servicio de autenticacion y emision de JWT para los accesos del ecosistema."),
            new("Microservicio Figuras", "http://localhost:3000/health/postgres", "Heartbeat de PostgreSQL consultado por gateway a traves de MicroservicioFiguras."),
            new("Gateway", "http://localhost:3000/health", "Capa de entrada para centralizar llamadas y enrutar peticiones entre servicios."),
            new("Frontend", "http://localhost:3000/health/frontend", "Cliente web principal conectado al backend a traves del gateway actual."),
            new("Logs Service", "http://localhost:3000/health/logs", "Heartbeat de LogsService consultado por gateway." ),
            new("HandsDetection", "http://localhost:3000/health/hands-detection", "Servicio de detección de manos (HandLandmarker) consultado por gateway.")
        ];
    }

    private async Task UpdateCardHeartbeatAsync(ServiceMonitorCard card)
    {
        if (string.IsNullOrWhiteSpace(card.Route))
        {
            card.SetHeartbeat(NeutralHeartbeatBrush, "Sin health dedicado", null, null);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await HealthClient.GetAsync(card.Route);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                card.SetHeartbeat(HealthyHeartbeatBrush, "En funcionamiento", stopwatch.ElapsedMilliseconds, null);
                return;
            }

            var responseMessage = await response.Content.ReadAsStringAsync();
            var errorMessage = string.IsNullOrWhiteSpace(responseMessage)
                ? $"El servicio respondio con estado {(int)response.StatusCode}."
                : responseMessage;

            card.SetHeartbeat(UnhealthyHeartbeatBrush, "Sin conexion", stopwatch.ElapsedMilliseconds, errorMessage);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            card.SetHeartbeat(UnhealthyHeartbeatBrush, "Sin conexion", stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ServiceMonitorCard card)
        {
            return;
        }

        var owner = Window.GetWindow(this);
        var intervalTextBox = new TextBox
        {
            Text = ((int)_heartbeatTimer.Interval.TotalSeconds).ToString(),
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(8),
            MinWidth = 100
        };

        var content = new StackPanel
        {
            Margin = new Thickness(24)
        };

        AddDetailRow(content, "Servicio", card.Name);
        AddDetailRow(content, "Estado actual", card.HeartbeatText);
        AddDetailRow(content, "Ultima verificacion", card.LastCheckDisplay);
        AddDetailRow(content, "Ultima verificacion exitosa", card.LastSuccessDisplay);
        AddDetailRow(content, "Endpoint consultado", card.RouteDisplay);
        AddDetailRow(content, "Tiempo de respuesta", card.ResponseTimeDisplay);
        AddDetailRow(content, "Fallos consecutivos", card.ConsecutiveFailures.ToString());
        AddDetailRow(content, "Ultimo mensaje de error", card.LastErrorDisplay);

        var intervalLabel = new TextBlock
        {
            Margin = new Thickness(0, 18, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Text = "Heartbeat cada cuantos segundos"
        };
        content.Children.Add(intervalLabel);
        content.Children.Add(intervalTextBox);
        content.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Foreground = Brushes.DimGray,
            Text = $"Rango permitido: {MinimumHeartbeatSeconds} a {MaximumHeartbeatSeconds} segundos."
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cerrar",
            Width = 100,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var applyButton = new Button
        {
            Content = "Aplicar",
            Width = 120
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(applyButton);
        content.Children.Add(buttonPanel);

        var detailsWindow = new Window
        {
            Title = $"Detalle de {card.Name}",
            Content = content,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 520,
            ResizeMode = ResizeMode.NoResize,
            Background = Brushes.White,
            Foreground = Brushes.Black
        };

        cancelButton.Click += (_, _) => detailsWindow.Close();
        applyButton.Click += (_, _) => ApplyHeartbeatInterval(detailsWindow, intervalTextBox.Text);

        detailsWindow.ShowDialog();
    }

    private async void VerifyStateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ServiceMonitorCard card || button.Tag is not string route)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(route))
        {
            MessageBox.Show($"{card.Name} no tiene health dedicado. Su estado se valida desde los servicios que dependen de el.", "Verificar estado", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        button.IsEnabled = false;

        try
        {
            using var response = await HealthClient.GetAsync(route);
            var message = response.IsSuccessStatusCode
                ? $"{card.Name} esta en funcionamiento."
                : $"{card.Name} no respondio correctamente.\n{await response.Content.ReadAsStringAsync()}";

            MessageBox.Show(message, "Verificar estado", MessageBoxButton.OK, response.IsSuccessStatusCode ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{card.Name} no esta disponible.\n{ex.Message}", "Verificar estado", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void ApplyHeartbeatInterval(Window detailsWindow, string rawValue)
    {
        if (!int.TryParse(rawValue, out var seconds) || seconds < MinimumHeartbeatSeconds || seconds > MaximumHeartbeatSeconds)
        {
            MessageBox.Show(
                $"Ingresa un numero entre {MinimumHeartbeatSeconds} y {MaximumHeartbeatSeconds} segundos.",
                "Heartbeat",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _heartbeatTimer.Interval = TimeSpan.FromSeconds(seconds);
        detailsWindow.Close();
    }

    private static void AddDetailRow(Panel panel, string label, string value)
    {
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.Black,
            Text = label
        });

        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            Text = value
        });
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private sealed class ServiceMonitorCard : INotifyPropertyChanged
    {
        private SolidColorBrush _heartbeatBrush = NeutralHeartbeatBrush;
        private string _heartbeatText = "Sin health dedicado";
        private DateTime? _lastCheckAt;
        private DateTime? _lastSuccessAt;
        private long? _responseTimeMs;
        private int _consecutiveFailures;
        private string? _lastErrorMessage;

        public ServiceMonitorCard(string name, string route, string description)
        {
            Name = name;
            Route = route;
            Description = description;

            if (!string.IsNullOrWhiteSpace(route))
            {
                _heartbeatBrush = UnhealthyHeartbeatBrush;
                _heartbeatText = "Verificando...";
            }
        }

        public string Name { get; }

        public string Route { get; }

        public string Description { get; }

        public DateTime? LastCheckAt
        {
            get => _lastCheckAt;
            private set
            {
                if (_lastCheckAt == value)
                {
                    return;
                }

                _lastCheckAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastCheckDisplay));
            }
        }

        public DateTime? LastSuccessAt
        {
            get => _lastSuccessAt;
            private set
            {
                if (_lastSuccessAt == value)
                {
                    return;
                }

                _lastSuccessAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastSuccessDisplay));
            }
        }

        public long? ResponseTimeMs
        {
            get => _responseTimeMs;
            private set
            {
                if (_responseTimeMs == value)
                {
                    return;
                }

                _responseTimeMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResponseTimeDisplay));
                OnPropertyChanged(nameof(HeartbeatSummary));
            }
        }

        public int ConsecutiveFailures
        {
            get => _consecutiveFailures;
            private set
            {
                if (_consecutiveFailures == value)
                {
                    return;
                }

                _consecutiveFailures = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeartbeatSummary));
            }
        }

        public string? LastErrorMessage
        {
            get => _lastErrorMessage;
            private set
            {
                if (_lastErrorMessage == value)
                {
                    return;
                }

                _lastErrorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastErrorDisplay));
            }
        }

        public string LastCheckDisplay => LastCheckAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Sin registros";

        public string LastSuccessDisplay => LastSuccessAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Sin registros";

        public string RouteDisplay => string.IsNullOrWhiteSpace(Route) ? "Sin endpoint configurado" : Route;

        public string ResponseTimeDisplay => ResponseTimeMs.HasValue ? $"{ResponseTimeMs.Value} ms" : "Sin medicion";

        public string LastErrorDisplay => string.IsNullOrWhiteSpace(LastErrorMessage) ? "Sin errores recientes" : LastErrorMessage;

        public string HeartbeatSummary => $"{(ResponseTimeMs.HasValue ? $"{ResponseTimeMs.Value} ms" : "Sin medicion")} · {ConsecutiveFailures} fallos";

        public SolidColorBrush HeartbeatBrush
        {
            get => _heartbeatBrush;
            private set
            {
                if (ReferenceEquals(_heartbeatBrush, value))
                {
                    return;
                }

                _heartbeatBrush = value;
                OnPropertyChanged();
            }
        }

        public string HeartbeatText
        {
            get => _heartbeatText;
            private set
            {
                if (_heartbeatText == value)
                {
                    return;
                }

                _heartbeatText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetHeartbeat(SolidColorBrush brush, string text, long? responseTimeMs, string? errorMessage)
        {
            HeartbeatBrush = brush;
            HeartbeatText = text;
            LastCheckAt = DateTime.Now;
            ResponseTimeMs = responseTimeMs;
            LastErrorMessage = errorMessage;

            if (ReferenceEquals(brush, HealthyHeartbeatBrush))
            {
                LastSuccessAt = LastCheckAt;
                ConsecutiveFailures = 0;
                LastErrorMessage = null;
                return;
            }

            if (!ReferenceEquals(brush, NeutralHeartbeatBrush))
            {
                ConsecutiveFailures++;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}