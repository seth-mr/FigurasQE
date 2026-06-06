using System.Windows;
using System.Windows.Controls;

namespace FQE.AdminClient.Views;

public partial class AutomatedTestsView : UserControl
{
    private readonly Action _goBack;

    public AutomatedTestsView(Action goBack)
    {
        _goBack = goBack;

        InitializeComponent();
        ServicesItemsControl.ItemsSource = CreateServices();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _goBack();
    }

    private static IReadOnlyList<AutomatedTestServiceCard> CreateServices()
    {
        return
        [
            new("MongoDB", "http://localhost:prueba", "Persistencia principal y origen de datos requerido por varios flujos criticos."),
            new("Auth Service", "http://localhost:prueba", "Servicio de autenticacion que mas adelante podria validar login, JWT y permisos de admin."),
            new("Microservicio Figuras", "http://localhost:prueba", "API central del dominio con escenarios futuros para CRUD, seguridad y reglas de negocio."),
            new("Gateway", "http://localhost:prueba", "Entrada unificada que podria probarse con contratos, enrutamiento y respuestas agregadas."),
            new("Frontend", "http://localhost:prueba", "Cliente web que podria participar en validaciones de integracion y disponibilidad externa."),
            new("Logs Service", "http://localhost:prueba", "Servicio Python que despues podria validarse con consultas, filtros y streaming de logs." )
        ];
    }

    private sealed record AutomatedTestServiceCard(string Name, string Route, string Description);
}